namespace SchreckLib.Networking.Events
{
    using System;
    using System.Net;
    using System.Text;
    using System.Net.Sockets;
    public class SendReceiveEventArgs : EventArgs
    {
        protected byte[] data;
        protected Socket socket;
        protected Base sender;
        public SendReceiveEventArgs(Socket socket, Base sender, byte[] data = null)
        {
            this.socket = socket;
            this.data = data;
            this.sender = sender;
        }
        public String getAddress()
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
        }
        public int getPort()
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Port;
        }
        public Socket getSocket()
        {
            return socket;
        }
        public string getText()
        {
            return Encoding.Unicode.GetString(data).Replace("\0", "");
        }
        public byte[] getBytes()
        {
            return data;
        }
        public void Send(byte[] data)
        {
            sender.Send(socket, data);
        }
        public void Send(string data)
        {
            Send(Encoding.Unicode.GetBytes(data));
        }

    }
}
