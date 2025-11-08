namespace MaksIT.LetsEncryptServer.Dto;

public class UserDto {
  public required string Id { get; set; }
  public required string Name { get; set; }
  public required string Salt { get; set; }
  public required string Hash { get; set; }
}
