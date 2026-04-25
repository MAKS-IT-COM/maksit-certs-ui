namespace MaksIT.Models.LetsEncryptServer.Common;

/// <summary>Vault-style paging (no org/app filters in this product).</summary>
public class PagedRequest {
  public int PageNumber { get; set; } = 1;
  public int PageSize { get; set; } = 20;
}
