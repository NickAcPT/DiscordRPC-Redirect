namespace DiscordRDPRedirect.Client
{
    using System;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    public class ClientHandler : ChannelHandlerAdapter
    {

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Console.WriteLine($"Internal connection succeeded. {context}");
            CurrentContext = context;
            base.ChannelActive(context);
        }

        public static IChannelHandlerContext CurrentContext { get; set; }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Console.WriteLine($"Removed internal connection. {context}");
            CurrentContext = null;
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is IByteBuffer byteBuffer)
            {
                var real = byteBuffer.ReadInt();
                if (real == 0)
                    return;
                var array = new byte[real];
                byteBuffer.ReadBytes(array);

                Console.WriteLine($"Received {real} bytes from server. Sending to proxy.");
                Client.PipeStream?.WriteAsync(array, 0, array.Length);
                Client.PipeStream?.FlushAsync();
                Console.WriteLine($"Sent {real} bytes to proxy.");
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