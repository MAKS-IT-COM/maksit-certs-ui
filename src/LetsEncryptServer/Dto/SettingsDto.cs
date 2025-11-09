namespace MaksIT.LetsEncryptServer.Dto;

public class SettingsDto {
  public required bool Init { get; set; }
  public required List<UserDto> Users { get; set; } = [];

}