namespace MaksIT.CertsUI.Engine.Dto.Certs;

/// <summary>PostgreSQL <c>app_runtime_leases</c> row for single-writer coordination.</summary>
public class AppRuntimeLeaseDto {
  public string LeaseName { get; set; } = "";
  public string HolderId { get; set; } = "";
  public long Version { get; set; }
  public DateTimeOffset AcquiredAtUtc { get; set; }
  public DateTimeOffset ExpiresAtUtc { get; set; }
}
