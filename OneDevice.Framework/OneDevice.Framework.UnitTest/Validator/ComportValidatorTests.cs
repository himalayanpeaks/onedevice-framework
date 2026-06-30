using OneDevice.Framework.Libs.Validator;

namespace OneDevice.Framework.UnitTest.Validator;

public class ComportValidatorTests
{
    private readonly ComportValidator _validator = new();

    [Theory]
    [InlineData("COM1")]
    [InlineData("COM23;19200")]
    [InlineData("COM3;115200")]
    public void Validate_ValidInputs_ReturnsTrue(string input)
    {
        Assert.True(_validator.Validate(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("com1")]
    [InlineData("COM")]
    [InlineData("COMX;9600")]
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
