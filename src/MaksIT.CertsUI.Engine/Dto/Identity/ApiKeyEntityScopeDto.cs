using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.CertsUI.Engine.Dto.Identity;

public class ApiKeyEntityScopeDto : DtoDocumentBase<Guid> {
  public required Guid ApiKeyId { get; set; }
  public required Guid EntityId { get; set; }
  public ScopeEntityType EntityType { get; set; }
  public ScopePermission Scope { get; set; }
}