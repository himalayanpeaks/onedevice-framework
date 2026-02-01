using OneDriver.Framework.Libs.Validator;
using System;
using Xunit;

namespace OneDriver.Framework.UnitTest.Validator;

public class ValidatorFactoryTests
{
    [Fact]
    public void CreateValidator_Comport_ReturnsComportValidator()
    {
        var v = ValidatorFactory.CreateValidator("Comport");
        Assert.IsType<ComportValidator>(v);
    }

    [Fact]
    public void CreateValidator_IPAddress_ReturnsIpAddressValidator()
    {
        var v = ValidatorFactory.CreateValidator("IPAddress");
        Assert.IsType<IpAddressValidator>(v);
    }

    [Fact]
    public void CreateValidator_Invalid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ValidatorFactory.CreateValidator("Unknown"));
    }
}
