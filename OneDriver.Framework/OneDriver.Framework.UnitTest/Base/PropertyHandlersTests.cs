using OneDriver.Framework.Base;
using Xunit;

namespace OneDriver.Framework.UnitTest.Base;

public class PropertyHandlersTests
{
    class TestHandlers : PropertyHandlers
    {
        private int _value;
        public int Value
        {
            get => GetProperty(ref _value);
            set => SetProperty(ref _value, value);
        }
    
        public new T GetProperty<T>(ref T field)
        {
            return base.GetProperty(ref field);
        }

        public T RequestProperty<T>(string propertyName)
        {
            return OnPropertyRequested<T>(propertyName);
        }

        public void RaiseReadRequested(string propertyName)
        {
            OnPropertyReadRequest(null!, propertyName);
        }
    }

    [Fact]
    public void SetProperty_InvokesPropertyChanged()
    {
        var obj = new TestHandlers();
        bool changed = false;
        obj.PropertyChanged += (s, e) => changed = true;

        obj.Value = 10;

        Assert.True(changed);
        Assert.Equal(10, obj.Value);
    }

    [Fact]
    public void GetProperty_ReadRequest_EventFires()
    {
        var obj = new TestHandlers();
        string? requested = null;
        obj.PropertyReadRequested += (s, e) => { requested = e.PropertyName; e.Value = 123; };

        var val = obj.RequestProperty<int>("Value");

        Assert.Equal("Value", requested);
        Assert.Equal(123, val);
    }
}
