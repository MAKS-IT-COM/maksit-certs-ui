using System.Management.Automation;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsCommunications.Connect, "CertsUI")]
[OutputType(typeof(void))]
public sealed class ConnectCertsUICmdlet : PSCmdlet {
  [Parameter(Mandatory = true, Position = 0)]
  public string BaseAddress { get; set; } = null!;

  [Parameter(Mandatory = true, Position = 1)]
  public string ApiKey { get; set; } = null!;

  protected override void ProcessRecord() {
    CertsUIConnectionState.SetConnection(BaseAddress, ApiKey);
    WriteVerbose($"Connected to CertsUI at {BaseAddress}");
  }
}
