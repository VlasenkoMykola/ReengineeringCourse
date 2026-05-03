using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void TranslateMessage_RoundTrip_ControlItem()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            var parameters = new byte[] { 0x01, 0xA0, 0x86, 0x01, 0x00, 0x00 };

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var parsedType, out var parsedCode, out var seqNum, out var parsedBody);

            //Assert
            Assert.That(success, Is.True);
            Assert.That(parsedType, Is.EqualTo(type));
            Assert.That(parsedCode, Is.EqualTo(code));
            Assert.That(seqNum, Is.EqualTo((ushort)0));
            Assert.That(parsedBody, Is.EqualTo(parameters));
        }

        [Test]
        public void GetSamples_Returns_Correct_16bit_Samples()
        {
            //Arrange — two 16-bit little-endian samples: 0x0102 and 0x0304
            var body = new byte[] { 0x02, 0x01, 0x04, 0x03 };

            //Act
            var samples = NetSdrMessageHelper.GetSamples(16, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(0x0102));
            Assert.That(samples[1], Is.EqualTo(0x0304));
        }

        [Test]
        public void GetSamples_Throws_On_Oversized_SampleSize()
        {
            //Arrange
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            //Act & Assert — sampleSize=40 exceeds 32-bit limit
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                NetSdrMessageHelper.GetSamples(40, body).ToList();
            });
        }

        [Test]
        public void GetSamples_Returns_Correct_24bit_Samples()
        {
            //Arrange — one 24-bit little-endian sample: bytes 0x03, 0x02, 0x01 → value 0x010203
            var body = new byte[] { 0x03, 0x02, 0x01 };

            //Act
            var samples = NetSdrMessageHelper.GetSamples(24, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(1));
            Assert.That(samples[0], Is.EqualTo(0x010203));
        }
    }
}