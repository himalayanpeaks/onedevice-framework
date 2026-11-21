using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OneDevice.Framework.Connection;

/// <summary>
///     A serial port with an optional baud rate, written as <c>PORT</c> or <c>PORT;BAUD</c>.
///     Accepts Windows (<c>COM3</c>), Linux (<c>/dev/ttyUSB0</c>, <c>/dev/ttyACM0</c>, <c>/dev/serial/by-id/...</c>)
///     and macOS (<c>/dev/cu.usbserial-1410</c>) port names.
/// </summary>
public sealed partial record SerialPortSettings : ConnectionSettings
{
    public const string Example = "COM3;9600";

    public SerialPortSettings(string portName, int? baudRate = null)
    {
        if (!IsValidPortName(portName))
            throw new ArgumentException($"'{portName}' is not a valid serial port name.", nameof(portName));
        if (baudRate is <= 0)
            throw new ArgumentOutOfRangeException(nameof(baudRate), baudRate, "Baud rate must be positive.");

        PortName = NormalizePortName(portName);
        BaudRate = baudRate;
    }

    /// <summary>Port name, e.g. <c>COM3</c> or <c>/dev/ttyUSB0</c>. Windows names are upper-cased.</summary>
    public string PortName { get; }

    /// <summary>Baud rate, or null when the device's default should be used.</summary>
    public int? BaudRate { get; }

    public static bool TryParse(string? text, [NotNullWhen(true)] out SerialPortSettings? settings,
        out string? error)
    {
        settings = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = $"Serial port is empty. Example: {Example}";
            return false;
        }

        var parts = text.Trim().Split(';');
        if (parts.Length > 2)
        {
            error = $"'{text}' has more than one ';'. Example: {Example}";
            return false;
        }

        var port = parts[0].Trim();
        if (!IsValidPortName(port))
        {
            error = $"'{port}' is not a serial port name (COMn, /dev/tty*, /dev/cu.*, /dev/serial/...). Example: {Example}";
            return false;
        }

        int? baud = null;
        if (parts.Length == 2 && parts[1].Trim().Length > 0)
        {
            if (!int.TryParse(parts[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var b) || b <= 0)
            {
                error = $"'{parts[1]}' is not a valid baud rate. Example: {Example}";
                return false;
            }

            baud = b;
        }

        settings = new SerialPortSettings(port, baud);
        error = null;
        return true;
    }

    public static new SerialPortSettings Parse(string text)
    {
        return TryParse(text, out var s, out var error) ? s : throw new FormatException(error);
    }

    /// <summary>Canonical init string: <c>PORT</c> or <c>PORT;BAUD</c>.</summary>
    public override string ToString()
    {
        return BaudRate is { } baud ? $"{PortName};{baud.ToString(CultureInfo.InvariantCulture)}" : PortName;
    }

    private static bool IsValidPortName(string? port)
    {
        return port != null && PortNameRegex().IsMatch(port);
    }

    private static string NormalizePortName(string port)
    {
        return port.StartsWith("com", StringComparison.OrdinalIgnoreCase) ? port.ToUpperInvariant() : port;
    }

    [GeneratedRegex(@"^(?:(?i:COM)[1-9][0-9]{0,2}|/dev/(?:tty|cu\.)[A-Za-z0-9._-]+|/dev/serial/[A-Za-z0-9/._:-]+)$")]
    private static partial Regex PortNameRegex();
}
