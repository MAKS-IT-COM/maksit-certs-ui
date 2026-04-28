using Microsoft.Extensions.Options;
using MaksIT.Core.Logging;
using MaksIT.Core.Webapi.Middlewares;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Extensions;
using MaksIT.CertsUI;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.CertsUI.HostedServices;
using MaksIT.CertsUI.Infrastructure;
using MaksIT.CertsUI.Mappers;
using MaksIT.CertsUI.Services;
using Npgsql;
using MaksIT.Results.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Extract configuration
var configuration = builder.Configuration;

// Logging
builder.Logging.AddConsoleLogger();

var appsettingsPath = Path.Combine(Path.DirectorySeparatorChar.ToString(), "configMap", "appsettings.json");
if (File.Exists(appsettingsPath)) {
  configuration.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: true);
}

// Load secrets from appsecrets.json: try Docker path first, then content-root-relative (local dev)
var secretsPaths = new[] {
  Path.Combine(Path.DirectorySeparatorChar.ToString(), "secrets", "appsecrets.json"),
  Path.Combine(builder.Environment.ContentRootPath ?? "", "secrets", "appsecrets.json"),
  Path.Combine(builder.Environment.ContentRootPath ?? "", "appsecrets.json")
};

foreach (var secretsPath in secretsPaths) {
  if (!string.IsNullOrEmpty(secretsPath) && File.Exists(secretsPath)) {
    configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: true);
    break;
  }
}

// Configure strongly typed settings objects
var configurationSection = configuration.GetSection("Configuration");
var appSettings = configurationSection.Get<Configuration>()
  ?? throw new InvalidOperationException("Required configuration section 'Configuration' is missing or invalid.");

// Allow configurations to be available through IOptions<Configuration>
builder.Services.Configure<Configuration>(configurationSection);

// Configure JSON serialization options for the API
static void ConfigureJsonSerializerOptions(JsonSerializerOptions options) {
  options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
  options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
}

// Add services to the container.
builder.Services.AddControllers()
  .AddJsonOptions(options => ConfigureJsonSerializerOptions(options.JsonSerializerOptions));

// MaksIT.Results ObjectResult uses the same behavior as Controllers for JSON serialization, so we need to configure it as well to ensure consistent behavior across the API.
builder.Services.AddOptions<JsonOptions>().Configure(o =>
  ConfigureJsonSerializerOptions(o.JsonSerializerOptions));

builder.Services.AddScoped<JwtAuthorizationFilter>();
builder.Services.AddScoped<JwtOrApiKeyAuthorizationFilter>();

// Hosted services: coordination/bootstrap lease, then renewal sweeps (each uses short-lived Postgres leases — symmetric pods).
builder.Services.AddHostedService<InitializationHostedService>();
builder.Services.AddHostedService<AutoRenewal>();

// PostgreSQL: prefer Configuration:CertsUIEngineConfiguration:ConnectionString in appsecrets.json; fallback ConnectionStrings:Certs for older files.
var certsConnectionString = appSettings.CertsUIEngineConfiguration.ConnectionString
  ?? builder.Configuration.GetConnectionString("Certs");
if (string.IsNullOrWhiteSpace(certsConnectionString))
  throw new InvalidOperationException(
    "PostgreSQL connection is required: set Configuration:CertsUIEngineConfiguration:ConnectionString in secrets (same pattern as MaksIT.Vault VaultEngineConfiguration:ConnectionString), or ConnectionStrings:Certs.");

var engineSection = appSettings.CertsUIEngineConfiguration;

// Identity / flow configuration must be registered before AddCertsEngine (engine domain services depend on pepper, etc.).
builder.Services.AddSingleton<IIdentityDomainConfiguration>(sp =>
  sp.GetRequiredService<IOptions<Configuration>>().Value.CertsUIEngineConfiguration.JwtSettingsConfiguration);
builder.Services.AddSingleton<ITwoFactorSettingsConfiguration>(sp =>
  sp.GetRequiredService<IOptions<Configuration>>().Value.CertsUIEngineConfiguration.TwoFactorSettingsConfiguration);
builder.Services.AddSingleton<ICertsFlowEngineConfiguration>(sp =>
  sp.GetRequiredService<IOptions<Configuration>>().Value.CertsUIEngineConfiguration);
builder.Services.AddSingleton<IDefaultAdminBootstrapConfiguration>(sp =>
  sp.GetRequiredService<IOptions<Configuration>>().Value.CertsUIEngineConfiguration.Admin);
// Single process-wide lease holder id (see IRuntimeInstanceId) — must stay Singleton for app_runtime_leases coherence.
builder.Services.AddSingleton<IRuntimeInstanceId, RuntimeInstanceIdProvider>();

// Register engine services
builder.Services.AddCertsEngine(new MaksIT.CertsUI.Engine.CertsEngineConfiguration {
  ConnectionString = certsConnectionString,
  AutoSyncSchema = engineSection.AutoSyncSchema,
  LetsEncryptProduction = engineSection.Production,
  LetsEncryptStaging = engineSection.Staging,
});

builder.Services.AddScoped<ICacheService, CacheService>();

// Controller services
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddHttpClient<ICertsFlowDomainService, CertsFlowDomainService>();
builder.Services.AddScoped<ICertsFlowService, CertsFlowService>();
builder.Services.AddHttpClient<IAgentService, AgentService>();
builder.Services.AddScoped<IAgentDeploymentService>(sp => (IAgentDeploymentService)sp.GetRequiredService<IAgentService>());
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<UserToResponseMapper>();
builder.Services.AddScoped<ApiKeyToResponseMapper>();
builder.Services.AddScoped<AccountToResponseMapper>();
builder.Services.AddScoped<IIdentityService, IdentityService>();

// Add CORS services to the container and configure to allow any origin
builder.Services.AddCors(options => {
  options.AddPolicy("AllowAllOrigins", policy => {
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
  });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
  options.SwaggerDoc("v1", new() { Title = "CertsUI API", Version = "v1" });
});

builder.Services.AddHealthChecks()
  .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

var app = builder.Build();

// FluentMigrator must complete before any IHostedService starts; bootstrap uses app_runtime_leases.
await app.Services.EnsureCertsEngineMigratedAsync();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.AddCertsEngineMigrations();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
}

// Use CORS policy in the pipeline
app.UseCors("AllowAllOrigins");

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
  Predicate = r => r.Tags.Contains("live")
});

app.MapGet("/health/ready", async (CancellationToken ct) => {
  try {
    await using var conn = new NpgsqlConnection(certsConnectionString);
    await conn.OpenAsync(ct);
    await using var cmd = new NpgsqlCommand("SELECT 1", conn);
    _ = await cmd.ExecuteScalarAsync(ct);
    return Results.Text("ready", "text/plain");
  }
  catch {
    return Results.StatusCode(503);
  }
});

await app.RunAsync();
