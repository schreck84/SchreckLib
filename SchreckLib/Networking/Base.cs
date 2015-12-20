[assembly: System.CLSCompliant(true)]
namespace SchreckLib.Networking
{
    using Utils;
    using Events;
    using Exceptions;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Linq;
    using System.Collections.Generic;
    public abstract class Base : IDisposable
    {
        #region "Events"
        #region "Deprecated"

        #endregion
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
        //protected byte[] lastDataReceived;
        //protected int lastDataReceivedLength;
        //protected byte[] lastDataSent;
        protected List<IPAddress> addresses;
        protected int port;
        protected Socket socket;
        protected IPEndPoint EndPoint
        {
            get
            {
                if (endpoint == null)
                    endpoint = GetNextEndPoint();
                return endpoint;
            }
        }
        //protected Exception exception;
        protected bool isClosing = false;
        #endregion
        private IPEndPoint endpoint;
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
        protected Base(string host, int port, AddressFamily addressFamily = AddressFamily.InterNetwork, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
        {
            this.addresses = Dns.GetHostAddresses(host).ToList();
            this.port = port;
            this.socket = new Socket(addressFamily, socketType, protocol);
        }

        protected Base(IPAddress ip, int port, AddressFamily addressFamily = AddressFamily.InterNetwork, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
            : this(ip.ToString(), port, addressFamily, socketType, protocol)
        { }

        ~Base()
        {
            isClosing = true;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch { }
            Dispose(false);
        }
        #endregion

        #region "Methods"
        #region "Protected"
        /// <summary>
        /// Constructs a new IPEndPoint using the next value in the __aAddressList property. If no addresses remain, an exception is thrown.
        /// </summary>
        /// <returns>next available endpoint to listen on</returns>
        /// <exception cref="NoMoreAddressesException">No more addresses are available</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        protected IPEndPoint GetNextEndPoint()
        {
            IPAddress temp = this.addresses.Where(addy => addy.AddressFamily == socket.AddressFamily).FirstOrDefault();

            if (temp == null)
            {
                throw new NoMoreAddressesException();
            }
            this.addresses.Remove(temp);
            return new IPEndPoint(temp, port);




            //int i = 0;
            //if (endPoint != null)
            //{
            //    i = Array.IndexOf<IPAddress>(addresses, endPoint.Address) + 1;
            //}
            //if (i > addresses.Length)
            //    throw new NoMoreAddressesException();
            //endPoint = new IPEndPoint(addresses[i], port);
            //// Check that the Address family for the address matches the socket.Bad things happen when you try
            //// to connect to an IPv6 EndPoint using an IPv4 socket
            //if (endPoint.AddressFamily != socket.AddressFamily)
            //    endPoint = getNextEndPoint();
            //return endPoint;
        }
        public virtual void Send(byte[] data)
        {
            try
            {
                //lastDataSent = data;

                OnBeforeSend(socket, data);

                socket.Send(data);

                OnAfterSend(socket, data);


            }
            catch (Exception e)
            {
                onException(e);
            }
        }

        public void Send(string data)
        {
            this.Send(Encoding.Unicode.GetBytes(data));
        }
        public void Send(Socket socket, byte[] data)
        {
            try
            {

                OnBeforeSend(socket, data);
                if (socket.Connected)
                {
                    socket.Send(data);
                }
                else
                {
                    OnDisconnect(socket);
                }

                OnAfterSend(socket, data);


            }
            catch (Exception e)
            {
                onException(e);
            }
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

                OnBeforeRead(socket);
                if (socket.Connected)
                {
                    bufferLength = socket.EndReceive(result);
                }
                else
                {
                    OnDisconnect(socket);
                    return;
                }

                //Normally happens on a disconnect
                if (bufferLength == 0 && !isClosing)
                {
                    OnDisconnect(socket);
                    return;
                }
                if (isClosing)
                    return;

                buffer = state.Buffer;

                OnAfterRead(socket, buffer);
                //lastDataReceived = buffer;
                //lastDataReceivedLength = state.Length;

                OnDataReceived(socket, buffer);

                if (socket.Connected)
                {
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, state);
                }
                else
                {
                    OnDisconnect(socket);
                }
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

        protected virtual void OnError(string error)
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
        protected virtual void OnBeforeSend(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = BeforeSend;
            if (h != null)
                h(this, new SendReceiveEventArgs(socket, this, data));
        }
        protected virtual void OnAfterSend(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = AfterSend;
            if (h != null)
                h(this, new SendReceiveEventArgs(socket, this, data));
        }
        protected virtual void OnBeforeRead(Socket socket)
        {
            EventHandler<SendReceiveEventArgs> h = BeforeRead;
            if (h != null)
                h(this, new SendReceiveEventArgs(socket, this));
        }
        protected virtual void OnAfterRead(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = AfterRead;
            if (h != null)
                h(this, new SendReceiveEventArgs(socket, this, data));
        }
        protected virtual void OnDataReceived(Socket socket, byte[] data)
        {
            EventHandler<SendReceiveEventArgs> h = DataReceived;
            if (h != null)
                h(this, new SendReceiveEventArgs(socket, this, data));
        }
        protected virtual void OnDisconnect(Socket socket)
        {
            EventHandler<DisconnectEventArgs> h = Disconnected;
            if (h != null)
                h(this, new DisconnectEventArgs(socket.Handle, 0));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            if (!disposedValue)
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }

        }
        #endregion


    }
}