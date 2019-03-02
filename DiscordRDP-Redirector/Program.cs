using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRDPRedirect
{
    class Program
    {
        private static void GetInputFromUser(string text, out string input, string onEmpty = "")
        {
            Console.Write($"{text}: ");
            input = Console.ReadLine();
            if (onEmpty != "" && string.IsNullOrEmpty(input))
                input = onEmpty;
        }

        private static void GetIntInputFromUser(string text, out int input, int onEmpty = 0)
        {
            Console.Write($"{text} [{onEmpty}]: ");
            var textI = Console.ReadLine();

            int.TryParse(textI, out input);
            if (string.IsNullOrEmpty(textI))
                input = onEmpty;
        }

        private static void GetBoolInputFromUser(string text, out bool input)
        {
            var hasValidName = false;
            do
            {
                Console.Write($"{text} (Y or N): ");
                var data = Console.ReadKey().KeyChar;
                input = (data == 'Y') || (data == 'y');
                Console.WriteLine();
                hasValidName = (data == 'Y') || (data == 'N') || (data == 'y') ||
                               (data == 'n');
            } while (!hasValidName);
        }

        static void Main(string[] args)
        {
            GetBoolInputFromUser("Has Discord client", out var isServer);
            GetIntInputFromUser("Pipe Number", out var pipe);

            if (isServer)
            {
                Server.Server.RunServerAsync(pipeNum: pipe).ConfigureAwait(false).GetAwaiter().GetResult();
                Console.WriteLine(
                    $"Server started in port {Server.Server.Port}. Press Enter in order to close it!");
                Console.ReadLine();
                Server.Server.StopServer();
            }
            else
            {
                GetInputFromUser("Host to connect", out var host);

                Task.Run(() => Client.Client.RunClientAsync(host));

                Console.WriteLine("Client connected to server. Press enter to disconnect!");
                Console.ReadLine();
                Client.Client.StopClient();
            }
        }
    }
}