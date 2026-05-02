using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Identity.User;

public class PatchUserRequest : PatchRequestModelBase {
  #region Master data Properties
  public string? Username { get; set; }
  public string? Email { get; set; }
  public string? MobileNumber { get; set; }
  public bool? IsActive { get; set; }
  #endregion

  #region Authentication properties
  public string? Password { get; set; }
  #endregion


  #region Two-factor authentication properties
  public bool? TwoFactorEnabled { get; set; }
  #endregion

  #region Authorization properties
  public bool? IsGlobalAdmin { get; set; }
  public List<PatchUserEntityScopeRequest>? EntityScopes { get; set; }
  #endregion

  public bool HasAnyOfFields(IEnumerable<string> fieldNames) {
    if (fieldNames == null)
      return false;

    foreach (var field in fieldNames) {
      if (string.IsNullOrEmpty(field))
        continue;

      // Check if the field is present in the patch operations dictionary
      if (Operations != null && Operations.ContainsKey(field))
        return true;

      // Check if the property value is non-null (for direct property assignment)
      var property = GetType().GetProperty(field);
      if (property != null && property.GetValue(this) != null)
        return true;
    }
    return false;
  }
}
