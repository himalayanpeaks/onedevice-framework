# OneDevice
OneDevice Framework

OneDevice Framework is a universal, extensible, and lightweight framework designed for rapid development of device-specific drivers. It provides a structured approach with predefined interfaces, base classes, and common utilities that significantly simplify integrating and communicating with various hardware devices.

Features

- Generic Interfaces: Abstract interfaces to standardize device communication.
- Base Classes: Common functionalities that reduce repetitive code in specific drivers.
- Extensible Design: Easily create and maintain device-specific drivers by inheriting provided base classes and interfaces.
- Dependency Injection Ready: Seamless integration into existing applications using DI frameworks.

## What's new in 2.5 — and how to migrate

2.5 adds new APIs next to the existing ones. Nothing breaks; the old APIs are marked `[Obsolete]` and will be removed in 3.0.

| Instead of (obsolete) | Use (2.5+) | Why |
|---|---|---|
| Throwing in a `PropertyChanging` handler | `e.Reject("Volts must be 0..30")` | Rejections are reported via `PropertyValidationFailed` and `INotifyDataErrorInfo` (UIs show them). Throwing still works. |
| `GetProperty<T>()` (read via event) | `ReadFromDevice<T>()` | Makes hardware reads explicit; `GetProperty(ref field)` only returns the field. |
| `DataTunnel<T>` (inherit) | `PollingDataSource<T>` (compose) | Dedicated thread, configurable interval, value-based change detection, errors reported, `await foreach` via `ReadAllAsync`, `ILogger`. |
| `IValidator` + regex / `ValidatorFactory` | `SerialPortSettings`, `TcpSettings`, `ConnectionSettings.TryParse` | Typed, parsed once; Linux/macOS serial ports, host names, IPv6, valid port range. `ToString()` gives the init string. |
| `RelayCommand`, `ValidatorViewModel` | CommunityToolkit.Mvvm | UI helpers leave the core framework in 3.0. |

```csharp
// Validation
PropertyChanging += (_, e) =>
{
    if (e.PropertyName == nameof(DesiredVolts) && (double)e.NewValue > MaxVolts)
        e.Reject($"Desired volts must be 0..{MaxVolts}");
};

// Polling by composition
_source = new PollingDataSource<Reading>(() => ReadFromPort(),
    new PollingDataSourceOptions { Interval = TimeSpan.FromMilliseconds(500), Name = "Kd3005p" }, logger);
_source.DataReceived += (_, reading) => Update(reading);
_source.Start();
// ...
await _source.StopAsync();

// Connection settings
if (!SerialPortSettings.TryParse(initString, out var port, out var error)) return error;
serialPort.PortName = port.PortName;
serialPort.BaudRate = port.BaudRate ?? 9600;
```
