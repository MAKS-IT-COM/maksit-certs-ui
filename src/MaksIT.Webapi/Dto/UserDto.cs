using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.Webapi.Dto;

public class UserDto : DtoDocumentBase<Guid> {
  public required string Name { get; set; } = string.Empty;
  public required string Salt { get; set; } = string.Empty;
  public required string Hash { get; set; } = string.Empty;
  public required List<JwtTokenDto> JwtTokens { get; set; } = [];
  public required DateTime LastLogin { get; set; }
}
