using Serilog;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OneDevice.Framework.Base;

/// <summary>
///     Base class for observable, validated parameters.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <see cref="SetProperty{T}" /> raises <see cref="PropertyChanging" /> first. Handlers reject a value with
///             <see cref="PropertyValidationEventArgs.Reject" /> (or by throwing). A rejected value is not stored,
///             <see cref="PropertyValidationFailed" /> is raised and the error is exposed through
///             <see cref="INotifyDataErrorInfo" /> so UIs (WPF, MAUI, Avalonia) can show it.
///         </item>
///         <item>
///             <see cref="ReadFromDevice{T}" /> asks <see cref="PropertyReadRequested" /> handlers for a live value,
///             typically read from hardware. <see cref="GetProperty{T}(ref T)" /> only returns the backing field.
///         </item>
///     </list>
/// </remarks>
public class PropertyHandlers : INotifyDataErrorInfo
{
    private readonly object _errorsLock = new();
    private Dictionary<string, string>? _errors;
    private EventHandler<DataErrorsChangedEventArgs>? _errorsChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyValidationEventHandler? PropertyChanging;

    /// <summary>
    ///     Raised when a <see cref="PropertyChanging" /> handler rejected a new value.
    ///     The property keeps its previous value; this event makes the rejection observable to callers and UIs.
    /// </summary>
    public event EventHandler<PropertyValidationFailedEventArgs>? PropertyValidationFailed;

    public event PropertyReadRequestedEventHandler? PropertyReadRequested;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanging(object newValue, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName != null)
            PropertyChanging?.Invoke(this, new PropertyValidationEventArgs(propertyName, newValue));
    }

    /// <summary>Reads a live value through <see cref="PropertyReadRequested" />.</summary>
    [Obsolete("Ambiguous with GetProperty(ref field), which only returns the backing field. Use ReadFromDevice<T>() instead.")]
    protected T GetProperty<T>([CallerMemberName] string? propertyName = null)
    {
        return ReadFromDevice<T>(propertyName);
    }

    /// <summary>
    ///     Reads a live value (typically from hardware) by raising <see cref="PropertyReadRequested" />.
    ///     A handler must set <see cref="PropertyReadRequestedEventArgs.Value" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">No handler supplied a value.</exception>
    protected T ReadFromDevice<T>([CallerMemberName] string? propertyName = null)
    {
        return OnPropertyRequested<T>(propertyName ?? string.Empty);
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
        throw new InvalidOperationException(
            $"No value was supplied for property '{propertyName}'. " +
            $"Attach a {nameof(PropertyReadRequested)} handler that sets {nameof(PropertyReadRequestedEventArgs)}.{nameof(PropertyReadRequestedEventArgs.Value)}.");
    }

    protected virtual void OnPropertyReadRequest(object newValue, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName != null) PropertyReadRequested?.Invoke(this, new PropertyReadRequestedEventArgs(propertyName));
    }

    /// <summary>
    ///     Validates and stores <paramref name="value" />.
    /// </summary>
    /// <returns>True if the value was stored; false if it was unchanged or rejected.</returns>
    public bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        var name = propertyName ?? string.Empty;
        Exception? rejection = null;
        if (value != null && propertyName != null)
        {
            var args = new PropertyValidationEventArgs(propertyName, value);
            try
            {
                PropertyChanging?.Invoke(this, args);
                if (args.IsRejected)
                    rejection = new PropertyValidationException(name, value,
                        args.ErrorMessage ?? $"Value '{value}' was rejected for '{name}'.");
            }
            catch (Exception ex)
            {
                rejection = ex;
            }
        }

        if (rejection != null)
        {
            // The value is rejected and the field keeps its previous value.
            Log.Error($"Validation failed for property '{propertyName}': {rejection.Message}");
            SetError(name, rejection.Message);
            PropertyValidationFailed?.Invoke(this, new PropertyValidationFailedEventArgs(name, value, rejection));
            return false; // Prevent field update
        }

        field = value;
        ClearError(name);
        OnPropertyChanged(propertyName);
        return true;
    }

    protected T GetProperty<T>(ref T field)
    {
        return field;
    }

    #region INotifyDataErrorInfo (explicit, so no public members are added to every parameter class)

    bool INotifyDataErrorInfo.HasErrors
    {
        get
        {
            lock (_errorsLock)
            {
                return _errors is { Count: > 0 };
            }
        }
    }

    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        lock (_errorsLock)
        {
            if (_errors == null) return Array.Empty<string>();
            if (string.IsNullOrEmpty(propertyName)) return _errors.Values.ToArray();
            return _errors.TryGetValue(propertyName, out var error) ? new[] { error } : Array.Empty<string>();
        }
    }

    event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
    {
        add => _errorsChanged += value;
        remove => _errorsChanged -= value;
    }

    private void SetError(string propertyName, string message)
    {
        lock (_errorsLock)
        {
            _errors ??= new Dictionary<string, string>();
            if (_errors.TryGetValue(propertyName, out var existing) && existing == message) return;
            _errors[propertyName] = message;
        }

        _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ClearError(string propertyName)
    {
        lock (_errorsLock)
        {
            if (_errors == null || !_errors.Remove(propertyName)) return;
        }

        _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    #endregion
}
