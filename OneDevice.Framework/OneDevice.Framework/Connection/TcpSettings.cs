using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace OneDevice.Framework.Connection;

/// <summary>
///     A network endpoint: <c>host</c> or <c>host:port</c>. The host can be an IPv4 address, a DNS name or an
///     IPv6 address (written <c>[::1]:502</c> when a port is given). Ports must be 1..65535.
/// </summary>
public sealed record TcpSettings : ConnectionSettings
{
    public const string Example = "192.168.1.10:502";

    public TcpSettings(string host, int? port = null)
    {
        if (!IsValidHost(host)) throw new ArgumentException($"'{host}' is not a valid host name or IP address.", nameof(host));
        if (port is < IPEndPoint.MinPort + 1 or > IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be 1..65535.");

        Host = host;
        Port = port;
    }

    /// <summary>IP address or DNS host name (IPv6 without brackets).</summary>
    public string Host { get; }

    /// <summary>TCP port, or null when the device's default port should be used.</summary>
    public int? Port { get; }

    public static bool TryParse(string? text, [NotNullWhen(true)] out TcpSettings? settings, out string? error)
    {
        settings = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = $"Network endpoint is empty. Example: {Example}";
            return false;
        }

        var s = text.Trim();
        string host;
        string? portText = null;

        if (s.StartsWith('['))
        {
            // [ipv6] or [ipv6]:port
            var close = s.IndexOf(']');
            if (close < 0)
            {
                error = $"'{s}' is missing ']'.";
                return false;
            }

            host = s[1..close];
            var rest = s[(close + 1)..];
            if (rest.Length > 0)
            {
                if (rest[0] != ':')
                {
                    error = $"Unexpected '{rest}' after ']' in '{s}'.";
                    return false;
                }

                portText = rest[1..];
            }

            if (!IPAddress.TryParse(host, out var ip) || ip.AddressFamily != AddressFamily.InterNetworkV6)
            {
                error = $"'{host}' is not an IPv6 address.";
                return false;
            }
        }
        else if (s.Count(c => c == ':') > 1)
        {
            host = s; // bare IPv6, no port
        }
        else
        {
            var colon = s.LastIndexOf(':');
            host = colon < 0 ? s : s[..colon];
            if (colon >= 0) portText = s[(colon + 1)..];
        }

        if (!IsValidHost(host))
        {
            error = $"'{host}' is not a valid host name or IP address. Example: {Example}";
            return false;
        }

        int? port = null;
        if (portText != null)
        {
            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var p)
                || p < 1 || p > IPEndPoint.MaxPort)
            {
                error = $"'{portText}' is not a valid port (1..65535). Example: {Example}";
                return false;
            }

            port = p;
        }

        settings = new TcpSettings(host, port);
        error = null;
        return true;
    }

    public static new TcpSettings Parse(string text)
    {
        return TryParse(text, out var s, out var error) ? s : throw new FormatException(error);
    }

    /// <summary>Canonical init string: <c>host</c>, <c>host:port</c> or <c>[ipv6]:port</c>.</summary>
    public override string ToString()
    {
        if (Port is not { } port) return Host;
        var host = Host.Contains(':') ? $"[{Host}]" : Host;
        return $"{host}:{port.ToString(CultureInfo.InvariantCulture)}";
    }

    private static bool IsValidHost(string? host)
    {
        if (string.IsNullOrEmpty(host)) return false;
        if (IPAddress.TryParse(host, out var ip))
            // IPAddress.TryParse accepts shorthand like "1" or "1.2"; require the full dotted form for IPv4.
            return ip.AddressFamily == AddressFamily.InterNetworkV6 || host.Count(c => c == '.') == 3;
        if (host.All(c => char.IsDigit(c) || c == '.')) return false; // e.g. 999.1.1.1
        return Uri.CheckHostName(host) == UriHostNameType.Dns;
    }
}
