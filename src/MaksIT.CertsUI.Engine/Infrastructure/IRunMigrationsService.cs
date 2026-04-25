namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Runs database migrations (e.g. FluentMigrator) at startup. Called from InitializationHostedService.
/// </summary>
public interface IRunMigrationsService {
  Task RunAsync(CancellationToken cancellationToken = default);
}
