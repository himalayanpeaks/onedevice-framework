using System.ComponentModel;

namespace OneDevice.Framework.Base;

public delegate void PropertyValidationEventHandler(object sender, PropertyValidationEventArgs e);

public class PropertyValidationEventArgs : PropertyChangedEventArgs
{
    /// <summary>
    ///     Arguments for validation event
    /// </summary>
    /// <param name="propertyName">Name of property which is in changing state but hasn't changed yet</param>
    /// <param name="newValue">value against which the property has to be validated</param>
    public PropertyValidationEventArgs(string propertyName, object newValue) : base(propertyName)
    {
        NewValue = newValue;
    }

    /// <summary>
    ///     value against which the property has to be validated
    /// </summary>
    public object NewValue { get; }

    /// <summary>True when a handler called <see cref="Reject" />.</summary>
    public bool IsRejected { get; private set; }

    /// <summary>Reason given to <see cref="Reject" />; null when the value was accepted.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    ///     Rejects <see cref="NewValue" />: the property keeps its previous value.
    ///     Preferred over throwing from a handler; throwing still works and is treated the same way.
    /// </summary>
    public void Reject(string errorMessage)
    {
        IsRejected = true;
        ErrorMessage ??= errorMessage;
    }
}
