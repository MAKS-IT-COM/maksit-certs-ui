namespace MaksIT.CertsUI.Client.Models;

/// <summary>Numeric values must match the API / MaksIT.Core <c>PatchOperation</c> enum.</summary>
public static class CertsUIPatchOperations {
  public const int SetField = 0;
  public const int RemoveField = 1;
  public const int AddToCollection = 2;
  public const int RemoveFromCollection = 3;
}
