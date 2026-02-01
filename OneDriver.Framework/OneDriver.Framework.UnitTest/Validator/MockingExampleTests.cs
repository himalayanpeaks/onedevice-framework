using Moq;
using OneDriver.Framework.Libs.Validator;
using Xunit;

namespace OneDriver.Framework.UnitTest.Validator;

public class MockingExampleTests
{
    [Fact]
    public void Mock_IValidator_VerifyValidateCalled()
    {
        var mock = new Mock<IValidator>(MockBehavior.Strict);
        mock.Setup(v => v.Validate("dummy")).Returns(true).Verifiable();

        var result = mock.Object.Validate("dummy");

        Assert.True(result);
        mock.Verify(v => v.Validate("dummy"), Times.Once);
    }
}
