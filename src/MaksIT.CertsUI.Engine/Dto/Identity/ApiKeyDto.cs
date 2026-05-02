using MaksIT.Core.Abstractions.Dto;


namespace MaksIT.CertsUI.Engine.Dto.Identity;

public class ApiKeyDto : DtoDocumentBase<Guid> {
  public required string ApiKey { get; set; }
  public string? Description { get; set; }
  public bool IsGlobalAdmin { get; set; }

  public DateTime CreatedAt { get; set; }
  public DateTime? ExpiresAt { get; set; }

  public List<ApiKeyEntityScopeDto> EntityScopes { get; set; } = [];
}
