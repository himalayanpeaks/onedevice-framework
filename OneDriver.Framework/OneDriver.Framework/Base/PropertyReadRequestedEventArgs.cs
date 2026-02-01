namespace OneDriver.Framework.Base;

public delegate void PropertyReadRequestedEventHandler(object sender, PropertyReadRequestedEventArgs e);

public sealed class PropertyReadRequestedEventArgs(string propertyName) : EventArgs
{
    public string PropertyName { get; } = propertyName;

    public object? Value { get; set; }

    public object GetValue()
    {
        if (Value is not null)
            return Value;

        throw new ArgumentNullException(PropertyName, "no value");
    }
}