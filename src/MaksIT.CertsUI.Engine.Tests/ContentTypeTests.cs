using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using Xunit;

namespace MaksIT.CertsUI.Engine.Tests;

public class ContentTypeTests
{
    [Fact]
    public void ContentType_defines_expected_values()
    {
        Assert.Equal(4, Enum.GetValues<ContentType>().Length);
    }
}
