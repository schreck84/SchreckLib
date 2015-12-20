namespace SchreckLib.Networking
{
    using Data;
    using Events;
    using Exceptions;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    public abstract class Common
    {
        #region "Events"
        #region "Send Events"
        /// <summary>
        /// Fires before data is sent to a Client/Server
        /// </summary>
        public event EventHandler<SendReceiveEventArgs> BeforeSend;
        /// <summary>
        /// Fires after data is sent to a Client/Server
        /// </summary>
        public event EventHandler<SendReceiveEventArgs> AfterSend;
        #endregion
        
        #region "Read Events"
        /// <summary>
        /// Fires before reading infomation from the socket
        /// </summary>
        public event EventHandler<SendReceiveEventArgs> BeforeRead;
        /// <summary>
        /// Fires after reading information from the socket
        /// </summary>
        public event EventHandler<SendReceiveEventArgs> AfterRead;
        public event EventHandler<SendReceiveEventArgs> DataReceived;
        #endregion
        
        #region "Error Handling"
        /// <summary>
        /// Fires on an error raised from the class
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorRaised;
        /// <summary>
        /// Fires when an exception is thrown
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionRaised;
        #endregion
       
        #region "Disconnect Event"
        /// <summary>
        /// Fires when a client is disconnected from the server
        /// <b>NOTE:</b> this event fires once for a Networking.Client instance and once for each client connected to a Networking.Server instance.
        /// </summary>
        public event EventHandler<DisconnectEventArgs> Disconnected;
        #endregion
        #endregion

        #region "Variables"
        #region "Protected"
        protected byte[] lastDataReceived;
        protected int lastDataReceivedLength;
        protected byte[] lastDataSent;
        protected IPAddress[] addresses;
        protected int port;
        protected Socket socket;
        protected IPEndPoint endPoint;
        protected Exception exception;
        protected bool isClosing = false;
        #endregion
        #endregion

        #region "Constructors/Destructor"
        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        /// <param name="host">Hostname or IPAddress as a string</param>
        /// <param name="port">Port to listen on</param>
        /// <param name="addressFamily"></param>
        /// <param name="socketType"></param>
        /// <param name="protocol"></param>
        public Common(string host, int port, AddressFamily addressFamily = AddressFamily.InterNetwork, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
        {
            this.addresses = Dns.GetHostAddresses(host);
            this.port = port;
            this.socket = new Socket(addressFamily, socketType, protocol);
            getNextEndPoint();
        }
        public Common(IPAddress ip, int port, AddressFamily addressFamily = AddressFamily.InterNetwork, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
            : this(ip.ToString(), port, addressFamily, socketType, protocol)
        { }
        ~Common()
        {
            isClosing = true;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception e)
            {
                //FIXME - Notify of exception
            }
        }
        #endregion

        #region "Methods"
        #region "Protected"
        /// <summary>
        /// Constructs a new IPEndPoint using the next value in the __aAddressList property. If no addresses remain, an exception is thrown.
        /// </summary>
        /// <returns>next available endpoint to listen on</returns>
        /// <exception cref="NoMoreAddressesException">No more addresses are available</exception>
        protected IPEndPoint getNextEndPoint()
        {
            int i = 0;
            if (endPoint != null)
            {
                i = Array.IndexOf<IPAddress>(addresses, endPoint.Address) + 1;
            }
            if (i > addresses.Length)
                throw new NoMoreAddressesException();
            endPoint = new IPEndPoint(addresses[i], port);
            // Check that the Address family for the address matches the socket.Bad things happen when you try
            // to connect to an IPv6 EndPoint using an IPv4 socket
            if (endPoint.AddressFamily != socket.AddressFamily)
                endPoint = getNextEndPoint();
            return endPoint;
        }
        public void Send(byte[] data)
        {
            try
            {
                lastDataSent = data;

                onBeforeSend(socket, data);

                socket.Send(data);

                onAfterSend(socket, data);
                

            }
            catch (Exception e)
            {
                onException(e);
            }
        }

        public void Send(string data)
        {
            this.Send(Encoding.ASCII.GetBytes(data));
        }

        /// <summary>
        /// The Asynchronous Function used for receiving data from a connection
        /// </summary>
        /// <param name="result">iAsyncResult passed from Socket.BeginAccept</param>
        protected void ReceiveCallback(IAsyncResult result)
        {
            if (Thread.CurrentThread.Name != "Receiving Thread")
                Thread.CurrentThread.Name = "Receiving Thread";

            int bufferLength;
            byte[] buffer;

            try
            {
                State state = (State)result.AsyncState;
                Socket socket = state.Socket;

                onBeforeRead(socket);

                bufferLength = socket.EndReceive(result);

                //Normally happens on a disconnect
                if (bufferLength == 0 && !isClosing)
                {
                    onDisconnect(socket);
                    return;
                }
                if (isClosing)
                    return;

                buffer = state.Buffer;

                onAfterRead(socket, buffer);
                lastDataReceived = buffer;
                lastDataReceivedLength = state.Length;

                onDataReceived(socket, lastDataReceived);

                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, state);
            }
            catch (SocketException e)
            {
                switch (e.ErrorCode)
                {
                    // List any exceptions to the default case rule here
                    // -------------------------------------------------
                    // 10054 - Client died (crash/end task)
                    case 10054:
                        bufferLength = 0;
                        break;
                    default:
                        onException(e);
                        break;
                }
            }
            catch (Exception e)
            {
                onException(e);
            }
        }
        #endregion
        #endregion

        protected virtual void onError(string error)
        {
            EventHandler<ErrorEventArgs> h = ErrorRaised;
            if (h != null)
                h(this, new ErrorEventArgs(error));
        }
        protected virtual void onException(Exception exception)
        {
            EventHandler<ExceptionEventArgs> h = ExceptionRaised;
            if (h != null)
                h(this, new ExceptionEventArgs(exception));
        }
        protected virtual void onBeforeSend(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = BeforeSend;
            if (h != null)
                h(this, new SendReceiveEventArgs((IPEndPoint)socket.RemoteEndPoint, data));
        }
        protected virtual void onAfterSend(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = AfterSend;
            if (h != null)
                h(this, new SendReceiveEventArgs((IPEndPoint)socket.RemoteEndPoint, data));
        }
        protected virtual void onBeforeRead(Socket socket)
        {
            EventHandler<SendReceiveEventArgs> h = BeforeRead;
            if (h != null)
                h(this, new SendReceiveEventArgs((IPEndPoint)socket.RemoteEndPoint));
        }
        protected virtual void onAfterRead(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = AfterRead;
            if (h != null)
                h(this, new SendReceiveEventArgs((IPEndPoint)socket.RemoteEndPoint, data));
        }
        protected virtual void onDataReceived(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = DataReceived;
            if (h != null)
                h(this, new SendReceiveEventArgs((IPEndPoint)socket.RemoteEndPoint, data));
        }
        protected virtual void onDisconnect(Socket socket)
        {
            EventHandler<DisconnectEventArgs> h = Disconnected;
            if (h != null)
                h(this, new DisconnectEventArgs((IPEndPoint)socket.RemoteEndPoint));
        }
    }
}