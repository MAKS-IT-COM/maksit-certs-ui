using System.Management.Automation;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsCommon.Get, "CertsUIAccounts")]
[OutputType(typeof(AccountResponse[]))]
public sealed class GetCertsUIAccountsCmdlet : CertsUICmdletBase {
  protected override void ProcessRecord() {
    try {
      var result = RequireClient().GetAccountsAsync().GetAwaiter().GetResult();
      WriteObject(result, true);
    }
    catch (Exception ex) {
      WriteApiError(ex);
    }
  }
}
