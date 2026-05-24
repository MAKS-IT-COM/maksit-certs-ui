using System.Management.Automation;
using MaksIT.CertsUI.Client.Models;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client.PowerShell;

[Cmdlet(VerbsLifecycle.Invoke, "CertsUIPatchAccount")]
[OutputType(typeof(AccountResponse))]
public sealed class InvokeCertsUIPatchAccountCmdlet : CertsUICmdletBase {
  [Parameter(Mandatory = true, Position = 0)]
  public Guid AccountId { get; set; }

  [Parameter]
  public string? Description { get; set; }

  [Parameter]
  public bool? IsDisabled { get; set; }

  protected override void ProcessRecord() {
    if (Description == null && !IsDisabled.HasValue) {
      WriteError(new ErrorRecord(
        new ArgumentException("Specify at least one of -Description or -IsDisabled."),
        "NoPatchFields",
        ErrorCategory.InvalidArgument,
        null));
      return;
    }

    try {
      var operations = new Dictionary<string, int>();
      var request = new PatchAccountRequest();
      if (Description != null) {
        operations["description"] = CertsUIPatchOperations.SetField;
        request.Description = Description;
      }
      if (IsDisabled.HasValue) {
        operations["isDisabled"] = CertsUIPatchOperations.SetField;
        request.IsDisabled = IsDisabled.Value;
      }
      request.Operations = operations;

      var result = RequireClient().PatchAccountAsync(AccountId, request).GetAwaiter().GetResult();
      WriteObject(result);
    }
    catch (Exception ex) {
      WriteApiError(ex);
    }
  }
}
