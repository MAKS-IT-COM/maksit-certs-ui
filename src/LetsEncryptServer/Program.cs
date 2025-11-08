using System.Text.Json.Serialization;
using MaksIT.Core.Logging;
using MaksIT.Core.Webapi.Middlewares;
using MaksIT.LetsEncrypt.Extensions;
using MaksIT.LetsEncryptServer;
using MaksIT.LetsEncryptServer.Services;
using MaksIT.LetsEncryptServer.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Extract configuration
var configuration = builder.Configuration;

// Add logging
builder.Logging.AddConsoleLogger();

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


// Add services to the container.
builder.Services.AddControllers()
  .AddJsonOptions(options => {
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
  });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Hosted services
builder.Services.AddHostedService<AutoRenewal>();
builder.Services.AddHostedService<Initialization>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
  app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
