using System.Management.Automation;
using MaksIT.CertsUI.Client;

namespace MaksIT.CertsUI.Client.PowerShell;

public abstract class CertsUICmdletBase : PSCmdlet {
  protected ICertsUIClient RequireClient() {
    var client = CertsUIConnectionState.Client;
    if (client == null) {
      throw new InvalidOperationException("Not connected. Run Connect-CertsUI -BaseAddress <url> -ApiKey <key> first.");
    }
    return client;
  }

  protected void WriteApiError(Exception ex) {
    WriteError(new ErrorRecord(ex, "CertsUIApiError", ErrorCategory.NotSpecified, null));
  }
}
