using OneDriver.Framework.Libs.Validator;
using Xunit;

namespace OneDriver.Framework.UnitTest.Validator;

public class IpAddressValidatorTests
{
    private readonly IpAddressValidator _validator = new();

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("192.168.1.1:8080")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255:65535")]
    public void Validate_ValidInputs_ReturnsTrue(string input)
    {
        Assert.True(_validator.Validate(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("999.999.999.999")]
    [InlineData("abc")]
    [InlineData("192.168.1")]
    [InlineData("192.168.1.1:999999")]
    public void Validate_InvalidInputs_ReturnsFalse(string input)
    {
        Assert.False(_validator.Validate(input));
    }

    [Fact]
    public void GetExample_ReturnsNonEmptyExample()
    {
        var example = _validator.GetExample();
        Assert.False(string.IsNullOrWhiteSpace(example));
    }
}
