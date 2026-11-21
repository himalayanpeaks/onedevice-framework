using System.Collections;
using System.ComponentModel;
using OneDevice.Framework.Base;

namespace OneDevice.Framework.UnitTest.Base;

public class ValidationResultTests
{
    private sealed class Channel : PropertyHandlers
    {
        private double _volts;

        public Channel()
        {
            PropertyChanging += (_, e) =>
            {
                if (e.PropertyName == nameof(Volts) && (double)e.NewValue > 30)
                    e.Reject("Volts must be 0..30");
            };
            PropertyReadRequested += (_, e) =>
            {
                if (e.PropertyName == nameof(MeasuredVolts)) e.Value = 12.5;
            };
        }

        public double Volts
        {
            get => GetProperty(ref _volts);
            set => SetProperty(ref _volts, value);
        }

        public double MeasuredVolts => ReadFromDevice<double>();
    }

    private static string[] Errors(INotifyDataErrorInfo info, string? property)
    {
        return info.GetErrors(property).Cast<string>().ToArray();
    }

    [Fact]
    public void Reject_KeepsValue_AndReportsMessage()
    {
        var ch = new Channel { Volts = 10 };
        PropertyValidationFailedEventArgs? failed = null;
        ch.PropertyValidationFailed += (_, e) => failed = e;

        ch.Volts = 50;

        Assert.Equal(10, ch.Volts);
        Assert.NotNull(failed);
        var error = Assert.IsType<PropertyValidationException>(failed!.Error);
        Assert.Equal("Volts must be 0..30", error.Message);
        Assert.Equal(50.0, error.AttemptedValue);
    }

    [Fact]
    public void NotifyDataErrorInfo_TracksAndClearsErrors()
    {
        var ch = new Channel();
        INotifyDataErrorInfo info = ch;
        var changes = new List<string?>();
        info.ErrorsChanged += (_, e) => changes.Add(e.PropertyName);

        ch.Volts = 50;
        Assert.True(info.HasErrors);
        Assert.Equal(new[] { "Volts must be 0..30" }, Errors(info, nameof(Channel.Volts)));
        Assert.Equal(new[] { "Volts must be 0..30" }, Errors(info, null));

        ch.Volts = 20;
        Assert.False(info.HasErrors);
        Assert.Empty(Errors(info, nameof(Channel.Volts)));
        Assert.Equal(new[] { nameof(Channel.Volts), nameof(Channel.Volts) }, changes);
    }

    [Fact]
    public void ErrorsChanged_NotRaisedForRepeatedSameError()
    {
        var ch = new Channel();
        INotifyDataErrorInfo info = ch;
        var count = 0;
        info.ErrorsChanged += (_, _) => count++;

        ch.Volts = 50;
        ch.Volts = 60; // different value, same message
        Assert.Equal(1, count);
    }

    [Fact]
    public void ThrowingHandler_StillWorks_AndSetsError()
    {
        var ch = new Channel();
        ch.PropertyChanging += (_, e) =>
        {
            if ((double)e.NewValue < 0) throw new ArgumentOutOfRangeException(e.PropertyName, "negative");
        };

        ch.Volts = -1;

        Assert.Equal(0, ch.Volts);
        Assert.True(((INotifyDataErrorInfo)ch).HasErrors);
    }

    [Fact]
    public void ReadFromDevice_UsesReadRequestedHandler()
    {
        Assert.Equal(12.5, new Channel().MeasuredVolts);
    }

    [Fact]
    public void NoNewPublicProperties_OnParameterClasses()
    {
        // INotifyDataErrorInfo is implemented explicitly so serializers and UIs don't see HasErrors etc.
        var publicProps = typeof(PropertyHandlers).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("HasErrors", publicProps);
    }
}
