namespace OneDevice.Framework.Base;

/// <summary>
///     Describes a value that was rejected by a <see cref="PropertyHandlers.PropertyChanging" /> handler.
/// </summary>
/// <param name="propertyName">Name of the property whose new value was rejected.</param>
/// <param name="attemptedValue">The value that was rejected.</param>
/// <param name="error">The exception thrown by the validation handler.</param>
public sealed class PropertyValidationFailedEventArgs(string propertyName, object? attemptedValue, Exception error)
    : EventArgs
{
    public string PropertyName { get; } = propertyName;

    public object? AttemptedValue { get; } = attemptedValue;

    public Exception Error { get; } = error;
}
