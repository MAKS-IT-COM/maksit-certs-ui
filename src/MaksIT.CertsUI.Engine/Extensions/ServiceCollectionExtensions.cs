using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.FluentMigrations;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistance.Mappers;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.CertsUI.Engine.Persistance.Services.Linq2Db;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;
using MaksIT.CertsUI.Engine.Services;


namespace MaksIT.CertsUI.Engine.Extensions;

/// <summary>
/// Registers Certs Engine services. Engine returns <see cref="MaksIT.Results.Result"/> / <see cref="MaksIT.Results.Result{T}"/> and propagates failures;
/// it does not call ToActionResult or produce HTTP. The Web API layer materializes errors to ProblemDetails via result.ToActionResult().
/// </summary>
public static class ServiceCollectionExtensions {
  public static void AddCertsEngine(this IServiceCollection services, ICertsEngineConfiguration certsEngineConfiguration) {

    services.AddSingleton(certsEngineConfiguration);

    if (string.IsNullOrWhiteSpace(certsEngineConfiguration.ConnectionString))
      throw new ArgumentException("Certs engine connection string is required for FluentMigrator (empty string uses connectionless/preview mode and will not create tables).", nameof(certsEngineConfiguration));

    // FluentMigrator: IRunMigrationsService invoked from Program.cs before RunAsync. Use .For.All() so version metadata
    // and migration discovery match in-process runner expectations (see FluentMigrator docs / #1062).
    services.AddFluentMigratorCore()
      .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(certsEngineConfiguration.ConnectionString)
        .ScanIn(typeof(BaselineCertsSchema).Assembly).For.All())
      .AddLogging(lb => lb.AddFluentMigratorConsole());
    services.AddScoped<IRunMigrationsService, RunMigrationsService>();
    services.AddScoped<ISchemaSyncService, SchemaSyncService>();

    // Linq2Db data connection for query services (and future repositories)
    services.AddScoped<ICertsDataConnectionFactory, CertsDataConnectionFactory>();

    #region Mappers
    services.AddScoped<UserMapper>();
    #endregion

    #region APIKey
    services.AddScoped<IAPIKeyPersistanceService, ApiKeyPersistanceServiceLinq2Db>();
    services.AddScoped<IApiKeyQueryService, ApiKeyQueryServiceLinq2Db>();
    services.AddScoped<IApiKeyEntityScopeQueryService, ApiKeyEntityScopeQueryServiceStub>();
    services.AddScoped<IApiKeyDomainService, ApiKeyDomainService>();
    #endregion

    #region Registration cache
    services.AddScoped<IRegistrationCachePersistanceService, RegistrationCachePersistanceServiceLinq2Db>();
    services.AddScoped<IAcmeHttpChallengePersistenceService, AcmeHttpChallengePersistenceServiceLinq2Db>();
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
    #endregion

    #region Identity
    services.AddScoped<IIdentityPersistanceService, IdentityPersistanceServiceLinq2Db>();
    services.AddScoped<IUserQueryService, UserQueryServiceLinq2Db>();
    services.AddScoped<IIdentityDomainService, IdentityDomainService>();
    #endregion

    #region ACME / Let's Encrypt
    services.AddSingleton<AcmeSessionStore>();
    services.AddHttpClient<ILetsEncryptService, LetsEncryptService>();
    #endregion
  }

  #region Host initialization helpers

  /// <summary>
  /// Runs FluentMigrator (versioned <c>Up()</c> migrations, expand-only policy: no dropping legacy columns) then add-only
  /// <see cref="ISchemaSyncService"/> when <see cref="ICertsEngineConfiguration.AutoSyncSchema"/> is true. Called from <c>Program.cs</c> before <c>RunAsync</c>.
  /// </summary>
  public static async Task EnsureCertsEngineMigratedAsync(this IServiceProvider serviceProvider) {
    await using var scope = serviceProvider.CreateAsyncScope();
    var run = scope.ServiceProvider.GetRequiredService<IRunMigrationsService>();
    await run.RunAsync();
    var schemaSync = scope.ServiceProvider.GetRequiredService<ISchemaSyncService>();
    await schemaSync.SyncSchemaAsync();
  }

  #endregion
}
