namespace SchreckLib.Networking
{
    using Events;
    using Exceptions;
    using Networking.Utils;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    public class Client : Base
    {
        public Client(string host, int port) : this(host, port, AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { }
        public Client(IPAddress host, int port) : this(host, port, AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { }
        public Client(string host, int port, AddressFamily addressFamily) : this(host, port, addressFamily, SocketType.Stream, ProtocolType.Tcp) { }
        public Client(IPAddress host, int port, AddressFamily addressFamily) : this(host, port, addressFamily, SocketType.Stream, ProtocolType.Tcp) { }
        public Client(string host, int port, AddressFamily addressFamily, SocketType socketType) : this(host, port, addressFamily, socketType, ProtocolType.Tcp) { }
        public Client(IPAddress host, int port, AddressFamily addressFamily, SocketType socketType) : this(host, port, addressFamily, socketType, ProtocolType.Tcp) { }
        public Client(string host, int port, AddressFamily addressFamily, SocketType socketType, ProtocolType protocol) : base(host, port, addressFamily, socketType, protocol) { }
        public Client(IPAddress host, int port, AddressFamily addressFamily, SocketType socketType, ProtocolType protocol) : base(host, port, addressFamily, socketType, protocol) { }
        //public Client(string host, int port, AddressFamily addressFamily, SocketType socketType, ProtocolType protocol)
        //    : base(host, port, addressFamily, socketType, protocol) { }
        //public Client(IPAddress host, int port, AddressFamily addressFamily, SocketType socketType, ProtocolType protocol)
        //    : base(host, port, addressFamily, socketType, protocol) { }

        #region "Events"
        #region "Deprecated"
        [Obsolete("There is no replacement. Don't call ")]
        public event EventHandler<ConnectEventArgs> BeforeConnect;
        protected virtual void OnBeforeConnect(Socket socket)
        {
            EventHandler<ConnectEventArgs> h = BeforeConnect;
            if (h != null)
            {
                h(this, new ConnectEventArgs(socket));
            }
        }
        [Obsolete("Use Connected instead")]
        public event EventHandler<ConnectEventArgs> AfterConnect;
        #endregion
        public event EventHandler<ConnectEventArgs> Connected;
        protected virtual void OnConnected(Socket socket)
        {
            ConnectEventArgs e = new ConnectEventArgs(socket);
            EventHandler<ConnectEventArgs> h = Connected;
            if (h != null)
            {
                h(this, e);
            }

            //TODO - Remove This
            h = AfterConnect;
            if (h != null)
            {
                h(this, e);
            }
        }
        #endregion


        public void Connect()
        {
            try
            {
                OnBeforeConnect(socket);
                socket.BeginConnect(this.EndPoint, ConnectCallback, null);
                OnConnected(socket);
            }
            catch (Exception e)
            {
                OnException(e);
            }
        }
        protected void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);
            OnConnected(socket);
            State state = new State()
            {
                Buffer = new byte[1024],
                Socket = socket
            };
            socket.BeginReceive(state.Buffer, 0, state.Length, SocketFlags.None, ReceiveCallback, state);
        }
        public void Disconnect()
        {
            socket.Disconnect(false);
            OnDisconnect(socket);
            socket.Close();
            socket = null;
        }
    }
}
