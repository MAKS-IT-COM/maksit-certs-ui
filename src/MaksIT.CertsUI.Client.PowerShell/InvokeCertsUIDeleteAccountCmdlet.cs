using System.Management.Automation;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsLifecycle.Invoke, "CertsUIDeleteAccount")]
[OutputType(typeof(void))]
public sealed class InvokeCertsUIDeleteAccountCmdlet : CertsUICmdletBase {
  [Parameter(Mandatory = true, Position = 0)]
  public Guid AccountId { get; set; }

  protected override void ProcessRecord() {
    try {
      RequireClient().DeleteAccountAsync(AccountId).GetAwaiter().GetResult();
    }
    catch (Exception ex) {
      WriteApiError(ex);
    }
  }
}
