using MaksIT.Webapi.Domain;
using Xunit;

namespace MaksIT.Webapi.Tests.Domain;

public class SettingsDomainTests
{
    [Fact]
    public void Initialize_creates_admin_user()
    {
        var pepper = "pepper";

        var result = new Settings().Initialize(pepper);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Init);
        Assert.Single(result.Value.Users);
        Assert.Equal("admin", result.Value.Users[0].Name);
    }

    [Fact]
    public void GetUserByName_returns_user_after_initialize()
    {
        var settings = new Settings().Initialize("p").Value!;

        var found = settings.GetUserByName("admin");

        Assert.True(found.IsSuccess);
        Assert.Equal("admin", found.Value!.Name);
    }

    [Fact]
    public void GetUserByName_when_missing_returns_not_found()
    {
        var settings = new Settings();

        var found = settings.GetUserByName("nope");

        Assert.False(found.IsSuccess);
    }
}
