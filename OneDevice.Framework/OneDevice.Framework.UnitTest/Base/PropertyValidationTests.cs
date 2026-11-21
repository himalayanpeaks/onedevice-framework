using OneDevice.Framework.Base;

namespace OneDevice.Framework.UnitTest.Base;

public class PropertyValidationTests
{
    private sealed class Limited : PropertyHandlers
    {
        private int _value;

        public Limited()
        {
            PropertyChanging += (_, e) =>
            {
                if (e.PropertyName == nameof(Value) && (int)e.NewValue > 10)
                    throw new ArgumentOutOfRangeException(nameof(Value), "max is 10");
            };
        }

        public int Value
        {
            get => GetProperty(ref _value);
            set => SetProperty(ref _value, value);
        }

        public bool TrySet(int value) => SetProperty(ref _value, value, nameof(Value));

        public T Request<T>(string name) => OnPropertyRequested<T>(name);
    }

    [Fact]
    public void RejectedValue_KeepsOldValue_AndRaisesPropertyValidationFailed()
    {
        var obj = new Limited { Value = 5 };
        PropertyValidationFailedEventArgs? failure = null;
        var changed = false;
        obj.PropertyValidationFailed += (_, e) => failure = e;
        obj.PropertyChanged += (_, _) => changed = true;

        obj.Value = 50;

        Assert.Equal(5, obj.Value);
        Assert.False(changed);
        Assert.NotNull(failure);
        Assert.Equal(nameof(Limited.Value), failure!.PropertyName);
        Assert.Equal(50, failure.AttemptedValue);
        Assert.IsType<ArgumentOutOfRangeException>(failure.Error);
    }

    [Fact]
    public void SetProperty_ReturnsFalse_WhenRejected()
    {
        var obj = new Limited();
        Assert.False(obj.TrySet(50));
        Assert.True(obj.TrySet(7));
        Assert.Equal(7, obj.Value);
    }

    [Fact]
    public void AcceptedValue_DoesNotRaisePropertyValidationFailed()
    {
        var obj = new Limited();
        var failed = false;
        obj.PropertyValidationFailed += (_, _) => failed = true;

        obj.Value = 3;

        Assert.False(failed);
        Assert.Equal(3, obj.Value);
    }

    [Fact]
    public void ReadRequest_WithoutValue_ThrowsInvalidOperation_WithPropertyName()
    {
        var obj = new Limited();
        var ex = Assert.Throws<InvalidOperationException>(() => obj.Request<int>("MaxVolts"));
        Assert.Contains("MaxVolts", ex.Message);
    }
}
