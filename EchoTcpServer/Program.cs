using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServer
{
    /// <summary>
    /// This program was designed for test purposes only
    /// Not for a review
    /// </summary>
    public class EchoServer
    {
        private readonly int _port;
        private TcpListener? _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public EchoServer(int port)
        {
            _port = port;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = Task.Run(() => HandleClientAsync(client.GetStream(), _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        /// <summary>
        /// Reads data from a stream and echoes it back. Extracted to accept any Stream for testability.
        /// </summary>
        public static async Task HandleClientAsync(Stream stream, CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer.AsMemory(), token)) > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            _cancellationTokenSource.Dispose();
            Console.WriteLine("Server stopped.");
        }

        public static async Task Main(string[] args)
        {
            EchoServer server = new EchoServer(5000);

            _ = Task.Run(() => server.StartAsync());

            string host = "127.0.0.1";
            int port = 60000;
            int intervalMilliseconds = 5000;

            using (var sender = new UdpTimedSender(host, port))
            {
                Console.WriteLine("Press any key to stop sending...");
                sender.StartSending(intervalMilliseconds);

                Console.WriteLine("Press 'q' to quit...");
                while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                {
                    // Waiting for user to press 'q'
                }

                sender.StopSending();
                server.Stop();
                Console.WriteLine("Sender stopped.");
            }
        }
    }

    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private Timer? _timer;

        public UdpTimedSender(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        private ushort _sequenceNumber = 0;

        /// <summary>
        /// Builds a UDP data message: 2-byte header [0x04, 0x84] + 2-byte sequence number + sample data.
        /// Extracted as a public static method for testability.
        /// </summary>
        public static byte[] BuildMessage(ushort sequenceNumber, byte[] samples)
        {
            return new byte[] { 0x04, 0x84 }
                .Concat(BitConverter.GetBytes(sequenceNumber))
                .Concat(samples)
                .ToArray();
        }

        private void SendMessageCallback(object? state)
        {
            try
            {
                Random rnd = new Random();
                byte[] samples = new byte[1024];
                rnd.NextBytes(samples);
                _sequenceNumber++;

                byte[] msg = BuildMessage(_sequenceNumber, samples);
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port} ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopSending();
                _udpClient.Dispose();
            }
        }
    }
}