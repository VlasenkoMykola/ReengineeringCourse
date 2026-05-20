using EchoTcpServer;

namespace EchoServerTests
{
    public class EchoServerHandleClientTests
    {
        [Test]
        public async Task HandleClientAsync_EchoesSingleMessage()
        {
            var input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
            using var stream = new MemoryStream();
            stream.Write(input, 0, input.Length);
            stream.Position = 0;

            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            var result = stream.ToArray();
            var echoed = result.Skip(input.Length).ToArray();
            Assert.That(echoed, Is.EqualTo(input));
        }

        [Test]
        public async Task HandleClientAsync_EchoesMultipleChunks()
        {
            var chunk1 = new byte[] { 0x01, 0x02, 0x03 };
            var chunk2 = new byte[] { 0x04, 0x05 };
            var combined = chunk1.Concat(chunk2).ToArray();

            using var stream = new MemoryStream();
            stream.Write(combined, 0, combined.Length);
            stream.Position = 0;

            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            var result = stream.ToArray();
            var echoed = result.Skip(combined.Length).ToArray();
            Assert.That(echoed, Is.EqualTo(combined));
        }

        [Test]
        public async Task HandleClientAsync_EmptyStream_WritesNothing()
        {
            using var stream = new MemoryStream();

            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            Assert.That(stream.Length, Is.EqualTo(0));
        }

        [Test]
        public async Task HandleClientAsync_RespectsLargePayload()
        {
            var input = new byte[4096];
            new Random(42).NextBytes(input);

            using var stream = new MemoryStream();
            stream.Write(input, 0, input.Length);
            stream.Position = 0;

            await EchoServer.HandleClientAsync(stream, CancellationToken.None);

            var result = stream.ToArray();
            var echoed = result.Skip(input.Length).ToArray();
            Assert.That(echoed, Is.EqualTo(input));
        }

        [Test]
        public async Task HandleClientAsync_CancelledToken_StopsEarly()
        {
            var input = new byte[] { 0x01, 0x02 };
            using var stream = new MemoryStream();
            stream.Write(input, 0, input.Length);
            stream.Position = 0;

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await EchoServer.HandleClientAsync(stream, cts.Token);

            Assert.That(stream.Length, Is.EqualTo(input.Length));
        }
    }

    public class UdpTimedSenderBuildMessageTests
    {
        [Test]
        public void BuildMessage_HasCorrectHeader()
        {
            ushort seq = 1;
            var samples = new byte[] { 0xAA, 0xBB };

            var msg = UdpTimedSender.BuildMessage(seq, samples);

            Assert.Multiple(() =>
            {
                Assert.That(msg[0], Is.EqualTo(0x04));
                Assert.That(msg[1], Is.EqualTo(0x84));
            });
        }

        [Test]
        public void BuildMessage_HasCorrectSequenceNumber()
        {
            ushort seq = 0x0A0B;
            var samples = new byte[] { 0xFF };

            var msg = UdpTimedSender.BuildMessage(seq, samples);

            var parsedSeq = BitConverter.ToUInt16(msg, 2);
            Assert.That(parsedSeq, Is.EqualTo(seq));
        }

        [Test]
        public void BuildMessage_HasCorrectTotalLength()
        {
            ushort seq = 5;
            var samples = new byte[1024];

            var msg = UdpTimedSender.BuildMessage(seq, samples);

            Assert.That(msg, Has.Length.EqualTo(2 + 2 + 1024));
        }

        [Test]
        public void BuildMessage_ContainsSamplesAtEnd()
        {
            ushort seq = 1;
            var samples = new byte[] { 0x11, 0x22, 0x33 };

            var msg = UdpTimedSender.BuildMessage(seq, samples);

            var tail = msg.Skip(4).ToArray();
            Assert.That(tail, Is.EqualTo(samples));
        }

        [Test]
        public void BuildMessage_EmptySamples_ReturnsHeaderAndSeqOnly()
        {
            ushort seq = 0;
            var samples = Array.Empty<byte>();

            var msg = UdpTimedSender.BuildMessage(seq, samples);

            Assert.That(msg, Has.Length.EqualTo(4));
        }

        [Test]
        public void BuildMessage_SequenceZero_IsValid()
        {
            var msg = UdpTimedSender.BuildMessage(0, new byte[] { 0x01 });

            var parsedSeq = BitConverter.ToUInt16(msg, 2);
            Assert.That(parsedSeq, Is.EqualTo(0));
        }

        [Test]
        public void BuildMessage_MaxSequence_IsValid()
        {
            var msg = UdpTimedSender.BuildMessage(ushort.MaxValue, new byte[] { 0x01 });

            var parsedSeq = BitConverter.ToUInt16(msg, 2);
            Assert.That(parsedSeq, Is.EqualTo(ushort.MaxValue));
        }
    }

    public class UdpTimedSenderLifecycleTests
    {
        [Test]
        public void StartSending_Twice_ThrowsInvalidOperation()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(10000);

            Assert.Throws<InvalidOperationException>(() => sender.StartSending(10000));

            sender.StopSending();
        }

        [Test]
        public void StopSending_WithoutStarting_DoesNotThrow()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            Assert.DoesNotThrow(() => sender.StopSending());
        }

        [Test]
        public void Dispose_WithoutStarting_DoesNotThrow()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000);
            Assert.DoesNotThrow(() => sender.Dispose());
        }

        [Test]
        public void Dispose_AfterStarting_DoesNotThrow()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(60000);
            Assert.DoesNotThrow(() => sender.Dispose());
        }
    }

    public class EchoServerConstructorTests
    {
        [Test]
        public void Constructor_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new EchoServer(0));
        }

        [Test]
        public void Stop_AfterConstruction_DoesNotThrow()
        {
            var server = new EchoServer(0);
            Assert.DoesNotThrow(() => server.Stop());
        }
    }
}