namespace MaksIT.CertsUI.Engine;

/// <summary>
/// Engine-level configuration: PostgreSQL connection string, optional add-only schema sync, and ACME directory URLs (single options object, same layering as other MaksIT engines).
/// </summary>
public interface ICertsEngineConfiguration {
  string ConnectionString { get; }

  /// <summary>When true, run add-only schema sync at startup after migrations. Default false in production.</summary>
  bool AutoSyncSchema { get; }

  /// <summary>Let's Encrypt production ACME directory URL (RFC 8555).</summary>
  string LetsEncryptProduction { get; }

  /// <summary>Let's Encrypt staging ACME directory URL.</summary>
  string LetsEncryptStaging { get; }
}
