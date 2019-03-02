using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace DiscordRDPRedirect.Server
{
    public class Server
    {
        public static NamedPipeClientStream PipeStream { get; set; }

        public static ushort Port { get; set; }

        public static bool IsConnected { get; set; }

        public static async Task RunServerAsync(ushort port = 25356, int pipeNum = 0)
        {
            Port = port;
            IsConnected = true;
            PipeStream = new NamedPipeClientStream(".", $"discord-ipc-{pipeNum}", PipeDirection.InOut,
                PipeOptions.Asynchronous);
            PipeStream.Connect();
            IEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
            IEventLoopGroup workerGroup = new MultithreadEventLoopGroup();

            var bootstrap = new ServerBootstrap();
            bootstrap.Group(bossGroup, workerGroup);

            bootstrap.Channel<TcpServerSocketChannel>();

            bootstrap
                .Option(ChannelOption.SoBacklog, 100)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;

                    pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                    pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                    pipeline.AddLast("handler", new ServerHandler());
                }));

            BoundChannel = await bootstrap.BindAsync(port);

            Task.Run(() =>
            {
                Action startReading = null;
                var buffer = new byte[2048];
                /*while (IsConnected)
                {*/
                startReading = delegate
                {
                    GC.Collect();
                    buffer = new byte[2048];
                    PipeStream.BeginRead(buffer, 0, buffer.Length, ar =>
                    {
                        try
                        {
                            var real = PipeStream.EndRead(ar);
                            var received = new byte[real];
                            if (real == 0)
                            {
                                startReading?.Invoke();
                                return;
                            }

                            Buffer.BlockCopy(buffer, 0, received, 0, real);

                            Task.Run(() =>
                            {
                                var buf = Unpooled.Buffer();

                                buf.WriteInt(received.Length);
                                buf.WriteBytes(received);
                                Console.WriteLine(
                                    $"Received {received.Length} bytes from the pipe. Sending to client.");
                                if (ServerHandler.CurrentClient != null)
                                {
                                    ServerHandler.CurrentClient?.WriteAndFlushAsync(buf);
                                    Console.WriteLine(
                                        $"Sent {received.Length} received bytes from the pipe to the client.");
                                }
                            });

                            GC.Collect();
                            Thread.Sleep(100);
                            startReading?.Invoke();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }, null);
                };
                /* Thread.Sleep(100);
                startReading();
             }*/
                startReading();
            });
        }

        public static async void StopServer()
        {
            if (BoundChannel != null) await BoundChannel?.CloseAsync();
            PipeStream?.Close();
            PipeStream?.Dispose();
        }

        public static IChannel BoundChannel { get; set; }
    }
}