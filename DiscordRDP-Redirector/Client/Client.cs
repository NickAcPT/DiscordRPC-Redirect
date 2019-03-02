using System;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace DiscordRDPRedirect.Client
{
    public class Client
    {
        public static NamedPipeServerStream PipeStream { get; set; }

        public static IChannel ClientChannel { get; set; }

        public static async Task RunClientAsync(string host, ushort port = 25356, int pipeNum = 0)
        {
            var group = new MultithreadEventLoopGroup();

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;
                    pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                    pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                    pipeline.AddLast("client", new ClientHandler());
                }));

            Console.WriteLine("Trying to connect.");
            ClientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));
            Console.WriteLine("Connected to host.");

            PipeStream = new NamedPipeServerStream($"discord-ipc-{pipeNum}", PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            void BeginReadPipe()
            {
                Console.WriteLine("Waiting for someone to connect to pipe.");
                PipeStream.WaitForConnection();
                Console.WriteLine("Connected to pipe.");

                //Reading from pipe
                Task.Run(() =>
                {
                    Action beginRead = null;
                    var buffer = new byte[2048];
                    beginRead = delegate
                    {
                        if (!PipeStream.IsConnected)
                        {
                            Task.Run((Action) BeginReadPipe);
                            return;
                        }

                        try
                        {
                            Console.WriteLine("Starting to read.");
                            PipeStream.BeginRead(buffer, 0, buffer.Length, ar =>
                            {
                                Console.WriteLine("Ending read.");
                                var real = PipeStream.EndRead(ar);
                                if (real == 0)
                                {
                                    Thread.Sleep(100);
                                    beginRead?.Invoke();
                                    return;
                                }

                                var received = new byte[real];
                                Console.WriteLine($"Read ended with {real} bytes read.");
                                Buffer.BlockCopy(buffer, 0, received, 0, real);

                                var buf = Unpooled.Buffer();
                                buf.WriteInt(received.Length);
                                buf.WriteBytes(received);

                                Console.WriteLine($"Received {received.Length} bytes from the proxy. Sending to server.");
                                ClientHandler.CurrentContext?.WriteAndFlushAsync(buf)
                                    .ConfigureAwait(false)
                                    .GetAwaiter()
                                    .GetResult();
                                Console.WriteLine($"Sent {received.Length} bytes to the server.");
                                Thread.Sleep(100);
                                beginRead?.Invoke();
                            }, null);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    };
                    beginRead();
                });
            }

            Task.Run((Action) BeginReadPipe);
        }

        public static void StopClient()
        {
            try
            {
                ClientChannel?.CloseAsync();
            }
            catch (Exception e)
            {
                // ignored
            }

            try
            {
                PipeStream?.Close();
            }
            catch (Exception e)
            {
                // ignored
            }

            try
            {
                PipeStream?.Disconnect();
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }
}