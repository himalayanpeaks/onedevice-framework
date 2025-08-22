namespace OneDriver.Framework.Module
{
    public class Definition
    {
        public enum DeviceError
        {
            NoError = 0,
            InvalidInitString,
            ConnectionError,
            DisconnectionError,
            InvalidParameter,
            InvalidCommand,
            InvalidResponse,
            Timeout,
            UnknownError,
            AlreadyConnected,
            DataIsNull
        }
        public enum DeviceState
        {
            Disconnected,
            Connecting,
            Connected,
            Error
        }

    }
}

