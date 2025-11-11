using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.Results;

namespace MaksIT.Webapi.Domain;

public class Settings : DomainObjectBase {
  public bool Init { get; set; }
  public List<User> Users { get; set; } = [];

  public Settings() { }

  public Result<Settings?> Initialize(string pepper) {
    var userResult = new User()
      .SetName("admin")
      .SetPassword("password", pepper);

    if (!userResult.IsSuccess || userResult.Value == null) {
      return userResult.ToResultOfType<Settings?>(_ => null);
    }

    Init = true;
    Users = [userResult.Value];

    return Result<Settings?>.Ok(this);
  }

  public Result<User?> GetUserById(Guid id) {
    var user = Users.FirstOrDefault(x => x.Id == id);
    if (user == null)
      return Result<User?>.NotFound(null, "User not found.");
    return Result<User?>.Ok(user);
  }

  public Result<User?> GetUserByName(string name) {
    var user = Users.FirstOrDefault(x => x.Name == name);
    if (user == null)
      return Result<User?>.NotFound(null, "User not found.");

    return Result<User?>.Ok(user);
  }

  public Result<User?> GetByJwtToken(string token) {
    var user = Users.FirstOrDefault(u => u.JwtTokens.Any(t => t.Token == token));
    if (user == null)
      return Result<User?>.NotFound(null, "User not found.");
    return Result<User?>.Ok(user);
  }

  public Result<User?> GetByRefreshToken(string refreshToken) {
    var user = Users.FirstOrDefault(u => u.JwtTokens.Any(t => t.RefreshToken == refreshToken));
    if (user == null)
        return Result<User?>.NotFound(null, "User not found for the provided refresh token.");

    return Result<User?>.Ok(user);
  }

  public Result<Settings?> CreateUser(string name, string password, string pepper) {
    var setPasswordResult = new User()
      .SetName(name)
      .SetPassword(password, pepper);

    if (!setPasswordResult.IsSuccess || setPasswordResult.Value == null)
      return setPasswordResult.ToResultOfType<Settings?>(_ => null);

    var user = setPasswordResult.Value;

    Users.Add(user);

    return Result<Settings?>.Ok(this);
  }

  public Settings UpsertUser(User user) {
    var existing = Users.FirstOrDefault(u => u.Id == user.Id);
    if (existing != null)
      Users.Remove(existing);
    Users.Add(user);
    return this;
  }

  public Settings UpsertUsers(List<User> users) {
    foreach (var user in users)
      UpsertUser(user);
    return this;
  }

  public Result<Settings?> RemoveUser(Guid userId) {
    var user = Users.FirstOrDefault(u => u.Id == userId);
    if (user == null)
      return Result<Settings?>.NotFound(null, "User not found.");
    Users.Remove(user);
    return Result<Settings?>.Ok(this);
  }

  public Result<Settings?> RemoveUsers(List<Guid> userIds) {
    foreach (var userId in userIds) {
      var removeResult = RemoveUser(userId);
      if (!removeResult.IsSuccess)
        return removeResult;
    }
    return Result<Settings?>.Ok(this);
  }
}