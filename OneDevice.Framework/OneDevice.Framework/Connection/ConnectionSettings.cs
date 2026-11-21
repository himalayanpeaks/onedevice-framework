using System.Diagnostics.CodeAnalysis;

namespace OneDevice.Framework.Connection;

/// <summary>
///     Typed, validated connection parameters. Replaces free-form init strings checked by a regex:
///     parse once with <see cref="TryParse" />, then pass the typed object around.
/// </summary>
/// <remarks>
///     <see cref="object.ToString" /> returns the canonical init string, so a parsed value can still be passed to
///     APIs that take a string (e.g. <c>Connect(string initString)</c>).
/// </remarks>
public abstract record ConnectionSettings
{
    private protected ConnectionSettings()
    {
    }

    /// <summary>Parses a serial port (<see cref="SerialPortSettings" />) or a network endpoint (<see cref="TcpSettings" />).</summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out ConnectionSettings? settings,
        out string? error)
    {
        if (SerialPortSettings.TryParse(text, out var serial, out _))
        {
            settings = serial;
            error = null;
            return true;
        }

        if (TcpSettings.TryParse(text, out var tcp, out _))
        {
            settings = tcp;
            error = null;
            return true;
        }

        settings = null;
        error = $"'{text}' is neither a serial port (e.g. {SerialPortSettings.Example}) " +
                $"nor a network endpoint (e.g. {TcpSettings.Example}).";
        return false;
    }

    /// <summary>Parses like <see cref="TryParse" /> and throws <see cref="FormatException" /> on invalid input.</summary>
    public static ConnectionSettings Parse(string text)
    {
        return TryParse(text, out var settings, out var error) ? settings : throw new FormatException(error);
    }
}
