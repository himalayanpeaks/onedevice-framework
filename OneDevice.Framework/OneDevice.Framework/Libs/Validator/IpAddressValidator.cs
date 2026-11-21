using System.Text.RegularExpressions;
using OneDevice.Framework.Connection;

namespace OneDevice.Framework.Libs.Validator;

/// <summary>
///     Validates <c>ip</c> or <c>ip:port</c> init strings: IPv4 and IPv6 (<c>[::1]:502</c>),
///     ports 1..65535. New code should use <see cref="TcpSettings" /> directly.
/// </summary>
public class IpAddressValidator : IValidator
{
    // IPv4 with optional port 1..65535. Validate() also accepts IPv6 and is authoritative.
    private static readonly Regex IpValidationRegex = new(
        @"^((25[0-5]|2[0-4][0-9]|[0-1]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[0-1]?[0-9][0-9]?)" +
        @"(:(6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[1-9][0-9]{0,3}))?$",
        RegexOptions.Compiled
    );

    public Regex ValidationRegex => IpValidationRegex;

    public bool Validate(string inputString)
    {
        // IP addresses only (host names are rejected, as before); TcpSettings adds IPv6 and the 1..65535 port range.
        return TcpSettings.TryParse(inputString, out var settings, out _)
               && System.Net.IPAddress.TryParse(settings.Host, out _);
    }

    public string GetExample()
    {
        return "192.168.1.1:8080 or 192.168.1.1";
    }
}
