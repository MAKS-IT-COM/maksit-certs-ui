using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.FluentMigrations;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistence.Mappers;
using MaksIT.CertsUI.Engine.Persistence.Services;
using Microsoft.Extensions.Logging;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;
using MaksIT.CertsUI.Engine.Services;
using MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;


namespace MaksIT.CertsUI.Engine.Extensions;

/// <summary>
/// Registers Certs Engine services. Engine returns <see cref="MaksIT.Results.Result"/> / <see cref="MaksIT.Results.Result{T}"/> and propagates failures;
/// it does not call ToActionResult or produce HTTP. The Web API layer materializes errors to ProblemDetails via result.ToActionResult().
/// </summary>
public static class ServiceCollectionExtensions {
  public static void AddCertsEngine(this IServiceCollection services, ICertsEngineConfiguration certsEngineConfiguration) {

    services.AddSingleton<ICertsEngineConfiguration>(certsEngineConfiguration);

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
    services.AddScoped<ICertsUIDataConnectionFactory, CertsUIDataConnectionFactory>();

    #region Mappers
    services.AddScoped<UserMapper>(sp => new UserMapper(sp.GetRequiredService<ICertsEngineConfiguration>().JwtSettingsConfiguration.PasswordPepper));
    #endregion


    #region APIKey
    services.AddScoped<IApiKeyPersistenceService, ApiKeyPersistenceServiceLinq2Db>();
    services.AddScoped<IApiKeyAuthorizationPersistenceService, ApiKeyAuthorizationPersistenceServiceLinq2Db>();
    services.AddScoped<IApiKeyQueryService, ApiKeyQueryServiceLinq2Db>();
    services.AddScoped<IApiKeyEntityScopeQueryService, ApiKeyEntityScopeQueryServiceLinq2Db>();
    services.AddScoped<IApiKeyDomainService, ApiKeyDomainService>();
    #endregion

    #region Registration cache
    services.AddScoped<IRegistrationCachePersistenceService, RegistrationCachePersistenceServiceLinq2Db>();
    services.AddScoped<IRegistrationCacheDomainService, RegistrationCacheDomainService>();
    services.AddScoped<IAcmeSessionPersistenceService, AcmeSessionPersistenceServiceLinq2Db>();
    services.AddScoped<IAcmeHttpChallengePersistenceService, AcmeHttpChallengePersistenceServiceLinq2Db>();
    services.AddScoped<ITermsOfServiceCachePersistenceService, TermsOfServiceCachePersistenceServiceLinq2Db>();
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
    #endregion

    #region Identity
    services.AddScoped<IIdentityPersistenceService, IdentityPersistenceServiceLinq2Db>();
    services.AddScoped<IUserAuthorizationPersistenceService, UserAuthorizationPersistenceServiceLinq2Db>();
    services.AddScoped<IIdentityQueryService, IdentityQueryServiceLinq2Db>();
    services.AddScoped<IUserEntityScopeQueryService, UserEntityScopeQueryServiceLinq2Db>();
    services.AddScoped<IIdentityDomainService>(sp => {
      var logger = sp.GetRequiredService<ILogger<IdentityDomainService>>();
      var identityPersistenceService = sp.GetRequiredService<IIdentityPersistenceService>();
      var userAuthorizationPersistenceService = sp.GetRequiredService<IUserAuthorizationPersistenceService>();
      var vaultEngineConfiguration = sp.GetRequiredService<ICertsEngineConfiguration>();
      var adminUser = vaultEngineConfiguration.Admin;
      var jwtSettingsConfiguration = vaultEngineConfiguration.JwtSettingsConfiguration;
      var twoFactorSettingsConfiguration = vaultEngineConfiguration.TwoFactorSettingsConfiguration;

      return new IdentityDomainService(
        logger,
        identityPersistenceService,
        userAuthorizationPersistenceService,
        vaultEngineConfiguration,
        adminUser,
        jwtSettingsConfiguration,
        twoFactorSettingsConfiguration);
    });
    #endregion

    #region ACME / Let's Encrypt
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
