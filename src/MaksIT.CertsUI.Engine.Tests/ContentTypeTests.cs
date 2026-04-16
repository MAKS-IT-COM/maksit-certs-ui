using MaksIT.LetsEncrypt.Entities;
using Xunit;

namespace MaksIT.LetsEncrypt.Tests;

public class ContentTypeTests
{
    [Fact]
    public void ContentType_defines_expected_values()
    {
        Assert.Equal(4, Enum.GetValues<ContentType>().Length);
    }
}
