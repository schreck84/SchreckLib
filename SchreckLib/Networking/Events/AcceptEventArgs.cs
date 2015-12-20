namespace SchreckLib.Networking.Events
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    public class AcceptEventArgs : EventArgs
    {
        Socket socket;
        int connections;
        public AcceptEventArgs(Socket socket, int connections)
        {
            this.socket = socket;
            this.connections = connections;
        }
        public Socket getSocket()
        {
            return socket;
        }
        public int getConnectionCount()
        {
            return connections;
        }
        public string getAddress()
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
        }
        public int getPort()
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Port;
        }
    }
}
