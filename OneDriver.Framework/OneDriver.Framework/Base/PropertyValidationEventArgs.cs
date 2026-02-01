using System.ComponentModel;

namespace OneDriver.Framework.Base;

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
}