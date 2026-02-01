using System.ComponentModel;
using System.Runtime.CompilerServices;
using Serilog;

namespace OneDriver.Framework.Base;

public class PropertyHandlers
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyValidationEventHandler? PropertyChanging;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanging(object newValue, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName != null)
            PropertyChanging?.Invoke(this, new PropertyValidationEventArgs(propertyName, newValue));
    }

    protected T GetProperty<T>([CallerMemberName] string? propertyName = null)
    {
        if (propertyName != null) return OnPropertyRequested<T>(propertyName);
        return OnPropertyRequested<T>(string.Empty);
    }

    protected virtual T OnPropertyRequested<T>([CallerMemberName] string? propertyName = null)
    {
        var e = new PropertyReadRequestedEventArgs(string.Empty);
        if (propertyName != null)
        {
            e = new PropertyReadRequestedEventArgs(propertyName);
            PropertyReadRequested?.Invoke(this, e);
        }

        if (e.Value != null) return (T)e.Value;
        throw new NullReferenceException();
    }

    public event PropertyReadRequestedEventHandler? PropertyReadRequested;

    protected virtual void OnPropertyReadRequest(object newValue, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName != null) PropertyReadRequested?.Invoke(this, new PropertyReadRequestedEventArgs(propertyName));
    }

    public bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        try
        {
            if (value != null) OnPropertyChanging(value, propertyName);
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            Log.Error($"Validation failed for property '{propertyName}': {ex.Message}");
            return false; // Prevent field update
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected T GetProperty<T>(ref T field)
    {
        return field;
    }
}