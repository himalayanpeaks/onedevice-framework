namespace OneDevice.Framework.Base;

/// <summary>
///     Describes a value that was rejected through <see cref="PropertyValidationEventArgs.Reject" />.
///     Reported in <see cref="PropertyValidationFailedEventArgs.Error" />.
/// </summary>
public sealed class PropertyValidationException(string propertyName, object? attemptedValue, string message)
    : Exception(message)
{
    public string PropertyName { get; } = propertyName;

    public object? AttemptedValue { get; } = attemptedValue;
}
