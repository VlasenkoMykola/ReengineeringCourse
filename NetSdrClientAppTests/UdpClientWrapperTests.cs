using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    public class UdpClientWrapperTests
    {
        [Test]
        public void Equals_SamePort_ReturnsTrue()
        {
            var wrapper1 = new UdpClientWrapper(5000);
            var wrapper2 = new UdpClientWrapper(5000);

            Assert.That(wrapper1, Is.EqualTo(wrapper2));
        }

        [Test]
        public void Equals_DifferentPort_ReturnsFalse()
        {
            var wrapper1 = new UdpClientWrapper(5000);
            var wrapper2 = new UdpClientWrapper(6000);

            Assert.That(wrapper1, Is.Not.EqualTo(wrapper2));
        }

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            var wrapper = new UdpClientWrapper(5000);

            Assert.That(wrapper, Is.Not.EqualTo(null));
        }

        [Test]
        public void Equals_DifferentType_ReturnsFalse()
        {
            var wrapper = new UdpClientWrapper(5000);
            Assert.That(wrapper.Equals("not a wrapper"), Is.False);
        }

        [Test]
        public void GetHashCode_SamePort_SameHash()
        {
            var wrapper1 = new UdpClientWrapper(5000);
            var wrapper2 = new UdpClientWrapper(5000);

            Assert.That(wrapper1.GetHashCode(), Is.EqualTo(wrapper2.GetHashCode()));
        }

        [Test]
        public void GetHashCode_DifferentPort_DifferentHash()
        {
            var wrapper1 = new UdpClientWrapper(5000);
            var wrapper2 = new UdpClientWrapper(6000);

            Assert.That(wrapper1.GetHashCode(), Is.Not.EqualTo(wrapper2.GetHashCode()));
        }

        [Test]
        public void Exit_DoesNotThrow()
        {
            var wrapper = new UdpClientWrapper(5000);
            Assert.DoesNotThrow(() => wrapper.Exit());
        }

        [Test]
        public void StopListening_WithoutStarting_DoesNotThrow()
        {
            var wrapper = new UdpClientWrapper(5000);
            Assert.DoesNotThrow(() => wrapper.StopListening());
        }
    }
}