using OneDevice.Framework.Connection;
using OneDevice.Framework.Libs.Validator;

namespace OneDevice.Framework.UnitTest.Connection;

public class ConnectionSettingsTests
{
    [Theory]
    [InlineData("COM3", "COM3", null)]
    [InlineData("com7;9600", "COM7", 9600)]
    [InlineData("COM23;19200", "COM23", 19200)]
    [InlineData("COM3;", "COM3", null)]
    [InlineData("/dev/ttyUSB0", "/dev/ttyUSB0", null)]
    [InlineData("/dev/ttyACM1;115200", "/dev/ttyACM1", 115200)]
    [InlineData("/dev/cu.usbserial-1410;9600", "/dev/cu.usbserial-1410", 9600)]
    [InlineData("/dev/serial/by-id/usb-FTDI_FT232R-if00-port0", "/dev/serial/by-id/usb-FTDI_FT232R-if00-port0", null)]
    public void SerialPort_Valid(string text, string port, int? baud)
    {
        Assert.True(SerialPortSettings.TryParse(text, out var s, out var error), error);
        Assert.Equal(port, s!.PortName);
        Assert.Equal(baud, s.BaudRate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("COM")]
    [InlineData("COM0")]
    [InlineData("COM3;fast")]
    [InlineData("COM3;0")]
    [InlineData("COM3;9600;8")]
    [InlineData("/dev/sda1")]
    [InlineData("192.168.1.1")]
    public void SerialPort_Invalid(string text)
    {
        Assert.False(SerialPortSettings.TryParse(text, out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Theory]
    [InlineData("COM3;9600")]
    [InlineData("/dev/ttyUSB0")]
    public void SerialPort_ToString_RoundTrips(string text)
    {
        Assert.Equal(text, SerialPortSettings.Parse(text).ToString());
    }

    [Theory]
    [InlineData("192.168.1.10", "192.168.1.10", null)]
    [InlineData("192.168.1.10:502", "192.168.1.10", 502)]
    [InlineData("10.0.0.1:65535", "10.0.0.1", 65535)]
    [InlineData("plc-01.factory.local:502", "plc-01.factory.local", 502)]
    [InlineData("localhost", "localhost", null)]
    [InlineData("[::1]:502", "::1", 502)]
    [InlineData("fe80::1", "fe80::1", null)]
    public void Tcp_Valid(string text, string host, int? port)
    {
        Assert.True(TcpSettings.TryParse(text, out var s, out var error), error);
        Assert.Equal(host, s!.Host);
        Assert.Equal(port, s.Port);
    }

    [Theory]
    [InlineData("192.168.1.1:99999")]
    [InlineData("192.168.1.1:0")]
    [InlineData("256.1.1.1")]
    [InlineData("1.2.3")]
    [InlineData("host:port")]
    [InlineData("[::1")]
    [InlineData("bad host")]
    [InlineData("")]
    public void Tcp_Invalid(string text)
    {
        Assert.False(TcpSettings.TryParse(text, out _, out _));
    }

    [Theory]
    [InlineData("10.0.0.1:502")]
    [InlineData("[fe80::1]:502")]
    [InlineData("plc.local")]
    public void Tcp_ToString_RoundTrips(string text)
    {
        Assert.Equal(text, TcpSettings.Parse(text).ToString());
    }

    [Fact]
    public void ConnectionSettings_PicksSerialOrTcp()
    {
        Assert.IsType<SerialPortSettings>(ConnectionSettings.Parse("COM3;9600"));
        Assert.IsType<TcpSettings>(ConnectionSettings.Parse("10.0.0.5:502"));
        Assert.False(ConnectionSettings.TryParse("not a thing!", out _, out var error));
        Assert.Contains("COM3;9600", error);
        Assert.Throws<FormatException>(() => ConnectionSettings.Parse("::bad::port::"));
    }

    [Fact]
    public void Records_CompareByValue()
    {
        Assert.Equal(SerialPortSettings.Parse("com3;9600"), new SerialPortSettings("COM3", 9600));
        Assert.Equal(TcpSettings.Parse("10.0.0.1:502"), new TcpSettings("10.0.0.1", 502));
    }

    [Theory]
    [InlineData("COM5;9600", "COM5")]
    [InlineData("/dev/ttyUSB0;115200", "/dev/ttyUSB0")]
    [InlineData("/dev/ttyUSB0", "/dev/ttyUSB0")]
    public void ComportValidator_AcceptsLinux_AndKeepsPortInGroup1(string text, string port)
    {
        var validator = new ComportValidator();
        Assert.True(validator.Validate(text));
        // Kd3005p and TmgMaster2 read the port name from group 1.
        Assert.Equal(port, validator.ValidationRegex.Match(text).Groups[1].Value);
    }

    [Theory]
    [InlineData("10.0.0.1:502", true)]
    [InlineData("plc.local:502", false)] // host names: use TcpSettings
    [InlineData("[::1]:502", true)]
    [InlineData("10.0.0.1:99999", false)]
    public void IpAddressValidator_UsesTcpSettings(string text, bool valid)
    {
        Assert.Equal(valid, new IpAddressValidator().Validate(text));
    }
}
