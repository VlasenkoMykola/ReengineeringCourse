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
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg.Length, Is.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(actualCode, Is.EqualTo((short)code));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg.Length, Is.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        [Test]
        public void TranslateMessage_RoundTrip_ControlItem()
        {
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            var parameters = new byte[] { 0x01, 0xA0, 0x86, 0x01, 0x00, 0x00 };

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var parsedType, out var parsedCode, out var seqNum, out var parsedBody);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(parsedType, Is.EqualTo(type));
                Assert.That(parsedCode, Is.EqualTo(code));
                Assert.That(seqNum, Is.EqualTo((ushort)0));
                Assert.That(parsedBody, Is.EqualTo(parameters));
            });
        }

        [Test]
        public void GetSamples_Returns_Correct_16bit_Samples()
        {
            var body = new byte[] { 0x02, 0x01, 0x04, 0x03 };

            var samples = NetSdrMessageHelper.GetSamples(16, body).ToList();

            Assert.That(samples, Has.Count.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(0x0102));
            Assert.That(samples[1], Is.EqualTo(0x0304));
        }

        [Test]
        public void GetSamples_Throws_On_Oversized_SampleSize()
        {
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                NetSdrMessageHelper.GetSamples(40, body).ToList();
            });
        }

        [Test]
        public void GetSamples_Returns_Correct_24bit_Samples()
        {
            var body = new byte[] { 0x03, 0x02, 0x01 };

            var samples = NetSdrMessageHelper.GetSamples(24, body).ToList();

            Assert.That(samples, Has.Count.EqualTo(1));
            Assert.That(samples[0], Is.EqualTo(0x010203));
        }

        [Test]
        public void GetControlItemMessage_AllControlItemCodes_Produce_ValidMessages()
        {
            var codes = new[]
            {
                NetSdrMessageHelper.ControlItemCodes.IQOutputDataSampleRate,
                NetSdrMessageHelper.ControlItemCodes.RFFilter,
                NetSdrMessageHelper.ControlItemCodes.ADModes,
                NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency,
            };

            foreach (var code in codes)
            {
                var msg = NetSdrMessageHelper.GetControlItemMessage(
                    NetSdrMessageHelper.MsgTypes.SetControlItem, code, new byte[] { 0x01 });

                bool success = NetSdrMessageHelper.TranslateMessage(
                    msg, out var parsedType, out var parsedCode, out _, out var body);

                Assert.Multiple(() =>
                {
                    Assert.That(success, Is.True, $"Failed for code {code}");
                    Assert.That(parsedCode, Is.EqualTo(code));
                    Assert.That(body.Length, Is.EqualTo(1));
                });
            }
        }

        [Test]
        public void TranslateMessage_RoundTrip_AllMsgTypes()
        {
            var controlTypes = new[]
            {
                NetSdrMessageHelper.MsgTypes.SetControlItem,
                NetSdrMessageHelper.MsgTypes.CurrentControlItem,
                NetSdrMessageHelper.MsgTypes.ControlItemRange,
                NetSdrMessageHelper.MsgTypes.Ack,
            };

            foreach (var type in controlTypes)
            {
                var msg = NetSdrMessageHelper.GetControlItemMessage(
                    type, NetSdrMessageHelper.ControlItemCodes.ReceiverState, new byte[] { 0xAA });

                bool success = NetSdrMessageHelper.TranslateMessage(
                    msg, out var parsedType, out _, out _, out _);

                Assert.That(parsedType, Is.EqualTo(type), $"Type mismatch for {type}");
            }
        }

        [Test]
        public void TranslateMessage_UnknownControlItemCode_ReturnsFalse()
        {
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                NetSdrMessageHelper.MsgTypes.SetControlItem,
                NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                new byte[] { 0x01 });

            // Corrupt the control item code bytes (bytes 2-3)
            msg[2] = 0xFF;
            msg[3] = 0xFF;

            bool success = NetSdrMessageHelper.TranslateMessage(
                msg, out _, out _, out _, out _);

            Assert.That(success, Is.False);
        }

        [Test]
        public void GetDataItemMessage_MaxLength_ProducesZeroHeader()
        {
            // DataItem with exactly 8192 bytes of payload → header length field becomes 0
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var payload = new byte[8192];

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, payload);

            var num = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var lengthField = num - ((int)type << 13);

            Assert.That(lengthField, Is.EqualTo(0));
        }

        [Test]
        public void GetControlItemMessage_Throws_On_Oversized_Message()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                NetSdrMessageHelper.GetControlItemMessage(
                    NetSdrMessageHelper.MsgTypes.SetControlItem,
                    NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                    new byte[8190]);
            });
        }

        [Test]
        public void GetSamples_EmptyBody_ReturnsEmpty()
        {
            var samples = NetSdrMessageHelper.GetSamples(16, Array.Empty<byte>()).ToList();
            Assert.That(samples, Has.Count.EqualTo(0));
        }

        [Test]
        public void GetSamples_32bit_Returns_Correct_Sample()
        {
            var body = BitConverter.GetBytes(42);

            var samples = NetSdrMessageHelper.GetSamples(32, body).ToList();

            Assert.That(samples, Has.Count.EqualTo(1));
            Assert.That(samples[0], Is.EqualTo(42));
        }

        [Test]
        public void TranslateMessage_DataItem_HasSequenceNumber()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var payload = new byte[100];

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, payload);

            bool success = NetSdrMessageHelper.TranslateMessage(
                msg, out var parsedType, out var itemCode, out var seqNum, out var body);

            Assert.Multiple(() =>
            {
                Assert.That(parsedType, Is.EqualTo(type));
                Assert.That(itemCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            });
        }

        [Test]
        public void GetControlItemMessage_EmptyParameters()
        {
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                NetSdrMessageHelper.MsgTypes.SetControlItem,
                NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                Array.Empty<byte>());

            bool success = NetSdrMessageHelper.TranslateMessage(
                msg, out _, out var code, out _, out var body);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(code, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.ReceiverState));
                Assert.That(body, Has.Length.EqualTo(0));
            });
        }
    }
}