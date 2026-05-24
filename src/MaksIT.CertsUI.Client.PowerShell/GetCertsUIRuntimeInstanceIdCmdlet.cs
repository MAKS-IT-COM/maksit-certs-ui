using System.Management.Automation;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsCommon.Get, "CertsUIRuntimeInstanceId")]
[OutputType(typeof(RuntimeInstanceIdResponse))]
public sealed class GetCertsUIRuntimeInstanceIdCmdlet : CertsUICmdletBase {
  protected override void ProcessRecord() {
    try {
      var result = RequireClient().GetRuntimeInstanceIdAsync().GetAwaiter().GetResult();
      WriteObject(result);
    }
    catch (Exception ex) {
      WriteApiError(ex);
    }
  }
}
