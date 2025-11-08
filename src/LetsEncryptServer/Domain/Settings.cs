using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.Results;

namespace MaksIT.LetsEncryptServer.Domain;

public class Settings : DomainObjectBase {
  public bool Init { get; set; }
  public List<User> Users { get; set; } = [];

  public Settings() {}

  public Result<Settings?> Initialize(string pepper) {
    var userResult = new User("admin")
      .SetPassword("password", pepper);

    if (!userResult.IsSuccess || userResult.Value == null) {
      return userResult.ToResultOfType<Settings?>(_ => null);
    }

    Init = true;
    Users = [userResult.Value];

    return Result<Settings?>.Ok(this);
  }

  public Result<User?> GetUserByName(string name) {

    var user = Users.FirstOrDefault(x => x.Name == name);

    if (user == null)
      return Result<User?>.NotFound(null, "User not found.");

    return Result<User?>.Ok(user);
  }

  public Result<Settings?> AddUser(string name, string password, string pepper) {
    var setPasswordResult = new User(name)
      .SetPassword(password, pepper);

    if (!setPasswordResult.IsSuccess || setPasswordResult.Value == null)
      return setPasswordResult.ToResultOfType<Settings?>(_ => null);
   
    var user = setPasswordResult.Value;

    Users.Add(user);
    
    return Result<Settings?>.Ok(this);
  }

  public Result<Settings?> RemoveUser(string name) {
    if (Users.Any(x => x.Name == name)) {
      Users = [.. Users.Where(u => u.Name != name)];
      return Result<Settings?>.Ok(this);
    }
    
    return Result<Settings?>.NotFound(null, "User not found.");
  }
}