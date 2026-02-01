using OneDriver.Framework.Libs.Announcer;
using System.Threading;
using Xunit;

namespace OneDriver.Framework.UnitTest.Announcer;

public class DataTunnelTests
{
    class TestData : BaseDataForAnnouncement { public int X { get; set; } }

    class TestTunnel : DataTunnel<TestData>
    {
        private bool _first = true;
        protected override void FetchDataForTunnel(ref TestData data)
        {
            if (_first)
            {
                data = new TestData { X = 1 };
                _first = false;
            }
            else
            {
                data = new TestData { X = 1 };
            }
        }

        public void ExposeStart() => StartAnnouncingData();
        public void ExposeStop() => StopAnnouncingData();
    }

    [Fact]
    public void StartStop_DoesNotThrow()
    {
        var t = new TestTunnel();
        t.ExposeStart();
        Thread.Sleep(10);
        t.ExposeStop();
        Assert.True(true);
    }
}
