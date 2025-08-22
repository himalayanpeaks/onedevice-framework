using OneDriver.Framework.Base;
using OneDriver.Framework.Module.Parameter;

namespace OneDriver.Framework.ModuleBuilder
{
    public interface IProcessData<T> where T : BaseProcessData
    {
        public T ProcessData { get; set; }
    }
}
