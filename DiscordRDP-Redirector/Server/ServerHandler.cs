namespace DiscordRDPRedirect.Server
{
    using System;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    public class ServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelActive(IChannelHandlerContext context)
        {
            CurrentClient = context;
            Console.WriteLine($"Someone connected to this server. {context}");
            base.ChannelActive(context);
        }

        public static IChannelHandlerContext CurrentClient { get; set; }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            CurrentClient = null;
            Console.WriteLine($"Someone disconnected to this server. {context}");
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {

            Console.WriteLine($"Received message from client {message.GetType().Name} ({message is IByteBuffer}).");
            if (message is IByteBuffer buffer)
            {
                var num = buffer.ReadInt();
                if (num == 0)
                    return;
                var array = new byte[num];
                buffer.ReadBytes(array);
                Server.PipeStream?.WriteAsync(array, 0, array.Length);
                Server.PipeStream?.FlushAsync();
                Console.WriteLine($"Reverse proxied {array.Length} bytes to the pipe.");
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }
}