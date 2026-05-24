using System.Management.Automation;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsCommunications.Disconnect, "CertsUI")]
[OutputType(typeof(void))]
public sealed class DisconnectCertsUICmdlet : PSCmdlet {
  protected override void ProcessRecord() {
    CertsUIConnectionState.ClearConnection();
    WriteVerbose("Disconnected from CertsUI.");
  }
}
