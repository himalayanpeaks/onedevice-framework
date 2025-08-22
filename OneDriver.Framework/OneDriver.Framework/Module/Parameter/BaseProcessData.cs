using OneDriver.Framework.Base;

namespace OneDriver.Framework.Module.Parameter
{
    public class BaseProcessData : PropertyHandlers, IParameter
    {
        private DateTime timeStamp;

        public DateTime TimeStamp { get => GetProperty(ref timeStamp); set => SetProperty(ref timeStamp, value); }
    }
}
