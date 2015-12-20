namespace SchreckLib.Networking.Events
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    public class ConnectEventArgs : EventArgs
    {
        protected Socket socket;
        public ConnectEventArgs(Socket socket) {
            this.socket = socket;
        }
        public Socket getSocket()
        {
            return socket;
        }
    }
}
