namespace MaksIT.CertsUI.Engine;

public sealed class CertsEngineConfiguration : ICertsEngineConfiguration {
  public required string ConnectionString { get; init; }

  /// <inheritdoc />
  public bool AutoSyncSchema { get; init; }

  /// <inheritdoc />
  public required string LetsEncryptProduction { get; init; }

  /// <inheritdoc />
  public required string LetsEncryptStaging { get; init; }
}
