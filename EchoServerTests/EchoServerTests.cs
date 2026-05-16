using EchoTcpServer;

namespace EchoServerTests
{
    public class EchoServerHandleClientTests
    {
        [Test]
        public async Task HandleClientAsync_EchoesSingleMessage()
        {
            // Arrange
            var input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
            using var stream = new MemoryStream();
            stream.Write(input, 0, input.Length);
            stream.Position = 0;

            // Act
            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            // Assert — read back what was written after the original input
            var result = stream.ToArray();
            var echoed = result.Skip(input.Length).ToArray();
            Assert.That(echoed, Is.EqualTo(input));
        }

        [Test]
        public async Task HandleClientAsync_EchoesMultipleChunks()
        {
            // Arrange — simulate two sequential writes by concatenating data
            var chunk1 = new byte[] { 0x01, 0x02, 0x03 };
            var chunk2 = new byte[] { 0x04, 0x05 };
            var combined = chunk1.Concat(chunk2).ToArray();

            using var stream = new MemoryStream();
            stream.Write(combined, 0, combined.Length);
            stream.Position = 0;

            // Act
            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            var echoed = result.Skip(combined.Length).ToArray();
            Assert.That(echoed, Is.EqualTo(combined));
        }

        [Test]
        public async Task HandleClientAsync_EmptyStream_WritesNothing()
        {
            // Arrange
            using var stream = new MemoryStream();
            // Empty — nothing to read

            // Act
            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            // Assert
            Assert.That(stream.Length, Is.EqualTo(0));
        }

        [Test]
        public async Task HandleClientAsync_RespectsLargePayload()
        {
            // Arrange — 4KB payload
            var input = new byte[4096];
            new Random(42).NextBytes(input);

            using var stream = new MemoryStream();
            stream.Write(input, 0, input.Length);
            stream.Position = 0;

            // Act
            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            var echoed = result.Skip(input.Length).ToArray();
            Assert.That(echoed, Is.EqualTo(input));
        }
    }

    public class UdpTimedSenderBuildMessageTests
    {
        [Test]
        public void BuildMessage_HasCorrectHeader()
        {
            // Arrange
            ushort seq = 1;
            var samples = new byte[] { 0xAA, 0xBB };

            // Act
            var msg = UdpTimedSender.BuildMessage(seq, samples);

            // Assert — first two bytes are the fixed header
            Assert.That(msg[0], Is.EqualTo(0x04));
            Assert.That(msg[1], Is.EqualTo(0x84));
        }

        [Test]
        public void BuildMessage_HasCorrectSequenceNumber()
        {
            // Arrange
            ushort seq = 0x0A0B;
            var samples = new byte[] { 0xFF };

            // Act
            var msg = UdpTimedSender.BuildMessage(seq, samples);

            // Assert — bytes 2-3 are the little-endian sequence number
            var parsedSeq = BitConverter.ToUInt16(msg, 2);
            Assert.That(parsedSeq, Is.EqualTo(seq));
        }

        [Test]
        public void BuildMessage_HasCorrectTotalLength()
        {
            // Arrange
            ushort seq = 5;
            var samples = new byte[1024];

            // Act
            var msg = UdpTimedSender.BuildMessage(seq, samples);

            // Assert — 2 header + 2 seq + 1024 samples = 1028
            Assert.That(msg.Length, Is.EqualTo(2 + 2 + 1024));
        }

        [Test]
        public void BuildMessage_ContainsSamplesAtEnd()
        {
            // Arrange
            ushort seq = 1;
            var samples = new byte[] { 0x11, 0x22, 0x33 };

            // Act
            var msg = UdpTimedSender.BuildMessage(seq, samples);

            // Assert — last 3 bytes match samples
            var tail = msg.Skip(4).ToArray();
            Assert.That(tail, Is.EqualTo(samples));
        }

        [Test]
        public void BuildMessage_EmptySamples_ReturnsHeaderAndSeqOnly()
        {
            // Arrange
            ushort seq = 0;
            var samples = Array.Empty<byte>();

            // Act
            var msg = UdpTimedSender.BuildMessage(seq, samples);

            // Assert — only header + seq = 4 bytes
            Assert.That(msg.Length, Is.EqualTo(4));
        }
    }

    public class EchoServerConstructorTests
    {
        [Test]
        public void Constructor_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new EchoServer(0));
        }

        [Test]
        public void Stop_AfterConstruction_DoesNotThrow()
        {
            // Arrange
            var server = new EchoServer(0);

            // Act & Assert — stopping without starting should not crash
            Assert.DoesNotThrow(() => server.Stop());
        }
    }
}
