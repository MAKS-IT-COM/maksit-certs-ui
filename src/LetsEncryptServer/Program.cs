using MaksIT.LetsEncryptServer;
using MaksIT.LetsEncrypt.Services;
using MaksIT.LetsEncryptServer.Services;
using MaksIT.LetsEncryptServer.BackgroundServices;
using MaksIT.LetsEncryptServer.Middlewares;
using MaksIT.LetsEncrypt.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Extract configuration
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


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

builder.Services.AddMemoryCache();

builder.Services.RegisterLetsEncrypt(appSettings);

builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<ICertsFlowService, CertsFlowService>();
builder.Services.AddSingleton<IAccountService, AccountService>();
builder.Services.AddHttpClient<IAgentService, AgentService>();
builder.Services.AddHostedService<AutoRenewal>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
  app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
else {
  // app.UseMiddleware<GlobalExceptionMiddleware>();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
