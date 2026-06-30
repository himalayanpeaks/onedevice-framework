namespace OneDevice.Framework.Base;

/// <summary>
///     Implement this interface when it is desired to verify the (passed) value of a property before changing
///     If the validation fails, an exception shall be thrown
/// </summary>
public interface IHasValidation
{
    public event PropertyValidationEventHandler PropertyChanging;
}