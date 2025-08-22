using System.Windows.Input;

namespace OneDriver.Framework.Module
{
    public interface IDeviceViewModel
    {
        string InitString { get; set; }
        ICommand ConnectCommand { get; }
        ICommand DisconnectCommand { get; }
    }
}
