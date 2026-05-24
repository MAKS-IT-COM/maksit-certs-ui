using System.Management.Automation;
using MaksIT.CertsUI.Client.Models;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsLifecycle.Invoke, "CertsUICreateAccount")]
[OutputType(typeof(AccountResponse))]
public sealed class InvokeCertsUICreateAccountCmdlet : CertsUICmdletBase {
  [Parameter(Mandatory = true, Position = 0)]
  public string Description { get; set; } = null!;

  [Parameter(Mandatory = true, Position = 1)]
  public string[] Contacts { get; set; } = null!;

  [Parameter(Mandatory = true, Position = 2)]
  public string ChallengeType { get; set; } = null!;

  [Parameter(Mandatory = true, Position = 3)]
  public string[] Hostnames { get; set; } = null!;

  [Parameter]
  public SwitchParameter IsStaging { get; set; }

  [Parameter]
  public SwitchParameter AgreeToS { get; set; }

  protected override void ProcessRecord() {
    try {
      var request = new PostAccountRequest {
        Description = Description,
        Contacts = Contacts,
        ChallengeType = ChallengeType,
        Hostnames = Hostnames,
        IsStaging = IsStaging.IsPresent,
        AgreeToS = AgreeToS.IsPresent,
      };
      var result = RequireClient().CreateAccountAsync(request).GetAwaiter().GetResult();
      WriteObject(result);
    }
    catch (Exception ex) {
      WriteApiError(ex);
    }
  }
}
