using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SchreckLib.Networking;

namespace ConsoleApplication1
{
    class Program
    {
        static Server svr;
        static Client cli;
        static Client cli2;
        static int cliPort = 0;
        static bool disconnected = false;

        static void Main(string[] args)
        {
            svr = new Server("127.0.0.1", 12700);
#pragma warning disable CS0618 // Type or member is obsolete
            svr.Listening += AfterListen;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            svr.ClientConnected += ClientConnect;
#pragma warning restore CS0618 // Type or member is obsolete
            svr.ReceivedData += Svr_DataReceived;
            svr.BeforeBroadcast += Svr_BeforeBroadcast;
            svr.AfterBroadcast += Svr_AfterBroadcast;
            svr.ClientDisconnected += Svr_Disconnected;
            svr.ExceptionRaised += Svr_ExceptionRaised;
            svr.ErrorRaised += Svr_ErrorRaised;

            cli = new Client("127.0.0.1", 12700);
#pragma warning disable CS0618 // Type or member is obsolete
            cli.Connected += ClientAfterConnect;
#pragma warning restore CS0618 // Type or member is obsolete
            cli.ReceivedData += Cli_DataReceived;
            cli.Disconnected += Cli_Disconnected;
            cli.ExceptionRaised += Cli_ExceptionRaised;
            cli.ErrorRaised += Cli_ErrorRaised;

            svr.Listen(10);

            System.Threading.Thread.Sleep(100);

            /* UPCOMING CHANGES FROM Feg
            // Method Chaining - PreBuild Execution Chain!!!!
            object something = cli.Connect().Then((sender, args) => { Console.WriteLine("I'm Connected!!!!"); });

            // Method Chaining - PreBuild Execution Chain!!!
            object somethingElse = cli.Disconnect().Then((sender, args) => { Console.WriteLine("I'm Disconnected!!!!"); });

            // Method Chaining - PreBuild Execution Chain, calls previously buit execution chain.
            var endless = cli.Connect().Then((sender, args) => { somethingElse.Execute(); });

            // Method Chaining - Re-assigning previously built chain to cause endless loop of connect/disconnect.
            somethingElse = cli.Disconnect().Then((sender, args) => { endless.Execute(); });

            // Begin the stored execution chain (endlessly connect and disconnect...
            endless.Execute();

            GameClient gc = new GameClient();

            cli.Connect().Then(GameClient.PostConnect).Then(GameClient.BeginStoredLogin).Then(SceneManager.ShowCharacterScreen);


            cli.Connect().Then((sender, args) => { Console.WriteLine("I'm Connected!!!!"); }).Execute();

            */

            Console.ReadKey();

            cli.Send("Some random message");

        }

        private static void Cli_ErrorRaised(object sender, SchreckLib.Networking.Events.ErrorEventArgs e)
        {
            Console.WriteLine("[Client1] ERROR: {0}", e.getError());
        }

        private static void Svr_ErrorRaised(object sender, SchreckLib.Networking.Events.ErrorEventArgs e)
        {
            Console.WriteLine("[SERVER] ERROR: {0}", e.getError());
        }

        private static void Cli_ExceptionRaised(object sender, SchreckLib.Networking.Events.ExceptionEventArgs e)
        {
            Console.WriteLine("[Client1] EXCEPTION: {0}", e.getException().Message);
        }

        private static void Svr_ExceptionRaised(object sender, SchreckLib.Networking.Events.ExceptionEventArgs e)
        {
            Console.WriteLine("[SERVER] EXCEPTION: {0}", e.getException().Message);
        }

        private static void Cli_Disconnected(object sender, SchreckLib.Networking.Events.DisconnectEventArgs e)
        {
            Console.WriteLine("[CLIENT1] Disconnected");
        }

        private static void Svr_Disconnected(object sender, SchreckLib.Networking.Events.ClientDisconnectEventArgs e)
        {
            Console.WriteLine("[SERVER] Client from {0}:{1} Disconnected (currently {2} client(s))", e.getAddress(), e.getPort(), e.getClientCount());
        }

        private static void Svr_AfterBroadcast(object sender, SchreckLib.Networking.Events.BroadcastEventArgs e)
        {
            Console.WriteLine("[SERVER] Successfully sent '{0}' to {1} client(s)", e.getText(), e.getClientCount());
        }

        private static void Svr_BeforeBroadcast(object sender, SchreckLib.Networking.Events.BroadcastEventArgs e)
        {
            Console.WriteLine("[SERVER] Broadcasting '{0}' to {1} client(s)", e.getText(), e.getClientCount());
        }

        private static void Svr_BeforeSend(object sender, SchreckLib.Networking.Events.SendReceiveEventArgs e)
        {
            if (!disconnected)
            {
                if (e.getPort() == cliPort)
                {
                    cli2.Disconnect();
                }
                else
                {
                    cli.Disconnect();
                }
                disconnected = true;
            }
        }

        private static void AfterListen(object sender, SchreckLib.Networking.Events.ListenEventArgs e)
        {
            Console.WriteLine("[SERVER] Listen: {0}:{1}", e.getAddress(), e.getPort().ToString());
            cli.Connect();
        }

        private static void ClientConnect(object sender, SchreckLib.Networking.Events.AcceptEventArgs e)
        {
            Console.WriteLine("[SERVER] A Client Connected from {0}:{1} (current Connections: {2})", e.getAddress(), e.getPort(), e.getConnectionCount());
            if (cliPort == 0)
            {
                cliPort = e.getPort();
            }

        }
        private static void ClientAfterConnect(object sender, SchreckLib.Networking.Events.ConnectEventArgs e)
        {
            Console.WriteLine("[Client1] Connected...");
            cli.Send("Hello Server!");
        }

        private static void Svr_DataReceived(object sender, SchreckLib.Networking.Events.SendReceiveEventArgs e)
        {
            Console.WriteLine("[Server] Data Recieved: '{0}'", e.getText().Trim());
            e.Send("Hello Client!");
        }

        private static void Cli_DataReceived(object sender, SchreckLib.Networking.Events.SendReceiveEventArgs e)
        {
            string response = e.getText();
            Console.WriteLine("[Client1] Data Recieved: '{0}'", response);
            if (response == "Hello Client!")
            {
                cli2 = new Client("127.0.0.1", 12700);
#pragma warning disable CS0618 // Type or member is obsolete
                cli2.AfterConnect += Cli2_AfterConnect;
#pragma warning restore CS0618 // Type or member is obsolete
                cli2.ReceivedData += Cli2_DataReceived;
                cli2.Disconnected += Cli2_Disconnected;
                cli2.ErrorRaised += Cli2_ErrorRaised;
                cli2.ExceptionRaised += Cli2_ExceptionRaised;
                cli2.Connect();
            }
        }

        private static void Cli2_ExceptionRaised(object sender, SchreckLib.Networking.Events.ExceptionEventArgs e)
        {
            Console.WriteLine("[Client2] EXCEPTION: {0}", e.getException().Message);
        }

        private static void Cli2_ErrorRaised(object sender, SchreckLib.Networking.Events.ErrorEventArgs e)
        {
            Console.WriteLine("[Client2] ERROR: {0}", e.getError());
        }

        private static void Cli2_Disconnected(object sender, SchreckLib.Networking.Events.DisconnectEventArgs e)
        {
            Console.WriteLine("[CLIENT2] Disconnected");
        }

        private static void Cli2_AfterConnect(object sender, SchreckLib.Networking.Events.ConnectEventArgs e)
        {
            Console.WriteLine("[Client2] Connected...");
            svr.Broadcast("Test Broadcast");
        }

        private static void Cli2_DataReceived(object sender, SchreckLib.Networking.Events.SendReceiveEventArgs e)
        {
            Console.WriteLine("[Client2] Data Recieved: '{0}'", e.getText().Trim());
            if (e.getText() == "Test Broadcast")
            {
                svr.BeforeSend += Svr_BeforeSend;
                svr.Broadcast("Test Broadcast 2 (should estimate 2, but only send to 1)");
            }
        }
    }
}
