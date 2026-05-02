using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.CertsUI.Engine.Dto.Identity;

public class UserEntityScopeDto : DtoDocumentBase<Guid> {
  public required Guid UserId { get; set; }
  public required Guid EntityId { get; set; }
  public ScopeEntityType EntityType { get; set; }
  public ScopePermission Scope { get; set; }
}