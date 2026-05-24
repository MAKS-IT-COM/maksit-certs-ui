using System.Management.Automation;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsDiagnostic.Test, "CertsUIHealth")]
[OutputType(typeof(void))]
public sealed class TestCertsUIHealthCmdlet : CertsUICmdletBase {
  protected override void ProcessRecord() {
    try {
      var client = RequireClient();
      client.CheckHealthLiveAsync().GetAwaiter().GetResult();
      client.CheckHealthReadyAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex) {
      WriteApiError(ex);
    }
  }
}
