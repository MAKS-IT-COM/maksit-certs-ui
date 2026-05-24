using System.Management.Automation;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsCommon.Get, "CertsUIAccount")]
[OutputType(typeof(AccountResponse))]
public sealed class GetCertsUIAccountCmdlet : CertsUICmdletBase {
  [Parameter(Mandatory = true, Position = 0)]
  public Guid AccountId { get; set; }

  protected override void ProcessRecord() {
    try {
      var result = RequireClient().GetAccountAsync(AccountId).GetAwaiter().GetResult();
      WriteObject(result);
    }
    catch (Exception ex) {
      WriteApiError(ex);
    }
  }
}
