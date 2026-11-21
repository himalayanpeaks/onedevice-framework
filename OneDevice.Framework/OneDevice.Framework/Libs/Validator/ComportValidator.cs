using System.Text.RegularExpressions;
using OneDevice.Framework.Connection;

namespace OneDevice.Framework.Libs.Validator;

/// <summary>
///     Validates <c>PORT</c> or <c>PORT;BAUD</c> init strings. Accepts Windows (<c>COM3</c>), Linux
///     (<c>/dev/ttyUSB0</c>) and macOS (<c>/dev/cu.usbserial-1410</c>) port names.
///     New code should use <see cref="SerialPortSettings" /> directly.
/// </summary>
public class ComportValidator : IValidator
{
    // Group 1 is the port name and group 2 the optional baud rate; HAL implementations read Groups[1].
    private static readonly Regex ComValidationRegex = new(
        @"^(COM\d+|/dev/(?:tty|cu\.)[A-Za-z0-9._-]+|/dev/serial/[A-Za-z0-9/._:-]+){1};?(\d+)?$",
        RegexOptions.Compiled);

    public Regex ValidationRegex => ComValidationRegex;

    public bool Validate(string inputString)
    {
        // Regex keeps the existing contract (upper-case COM); the typed parser adds range checks (no COM0, baud > 0).
        return !string.IsNullOrEmpty(inputString) && ValidationRegex.IsMatch(inputString)
               && SerialPortSettings.TryParse(inputString, out _, out _);
    }

    public string GetExample()
    {
        return "COM23;19200";
    }
}
