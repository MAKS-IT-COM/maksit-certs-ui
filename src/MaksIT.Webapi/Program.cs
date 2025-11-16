using MaksIT.Core.Logging;
using MaksIT.Core.Webapi.Middlewares;
using MaksIT.LetsEncrypt.Extensions;
using MaksIT.Webapi;
using MaksIT.Webapi.Authorization.Filters;
using MaksIT.Webapi.BackgroundServices;
using MaksIT.Webapi.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

#region Configuration setup
var configuration = builder.Configuration;

var configMapPath = Path.Combine(Path.DirectorySeparatorChar.ToString(), "configMap", "appsettings.json");
if (File.Exists(configMapPath)) {
  configuration.AddJsonFile(configMapPath, optional: false, reloadOnChange: true);
}

var secretsPath = Path.Combine(Path.DirectorySeparatorChar.ToString(), "secrets", "appsecrets.json");
if (File.Exists(secretsPath)) {
  configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: true);
}

// Configure strongly typed settings objects
var configurationSection = configuration.GetSection("Configuration");
var appSettings = configurationSection.Get<Configuration>() ?? throw new ArgumentNullException();

// Allow configurations to be available through IOptions<Configuration>
builder.Services.Configure<Configuration>(configurationSection);
#endregion

// Add logging
builder.Logging.AddConsoleLogger();

// Add services to the container.
builder.Services.AddControllers()
  .AddJsonOptions(options => {
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
  });

// Add custom authorization filter
builder.Services.AddScoped<JwtAuthorizationFilter>();

#region Swagger
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endregion

builder.Services.AddCors();

builder.Services.AddMemoryCache();

builder.Services.RegisterLetsEncrypt(appSettings);

#region Work with files concurrently
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
#endregion

builder.Services.AddHttpClient<ICertsFlowService, CertsFlowService>();
builder.Services.AddHttpClient<IAgentService, AgentService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();

#region Hosted services
builder.Services.AddHostedService<AutoRenewal>();
builder.Services.AddHostedService<Initialization>();
#endregion

// Add CORS services to the container and configure to allow any origin
builder.Services.AddCors(options => {
  options.AddPolicy("AllowAllOrigins", policy =>
  {
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
  });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
}

// Use CORS policy in the pipeline
app.UseCors("AllowAllOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();
