namespace SchreckLib.Networking
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Net.Sockets;
    using System.Net;
    using System.Text;
    using System.Linq;
    using Events;
    using Utils;
    public class Server : Base, IDisposable
    {

        #region "Events"
        #region "Deprecated Events"
        [Obsolete("Dont call Server.Listen() before you are ready")]
        public event EventHandler<ListenEventArgs> BeforeListen;
        [Obsolete("There is no substitute")]
        protected virtual void OnBeforeListen(IPEndPoint endpoint, int backlog)
        {
            EventHandler<ListenEventArgs> h = BeforeListen;
            if (h != null)
                h(this, new ListenEventArgs(endpoint, backlog));
        }
        [Obsolete("Use Listening instead")]
        public event EventHandler<ListenEventArgs> AfterListen;
        [Obsolete("Use ClientConnected")]
        public event EventHandler<AcceptEventArgs> Connected;
        #endregion
        public event EventHandler<ListenEventArgs> Listening;
        protected virtual void OnListening(IPEndPoint endpoint, int backlog)
        {
            ListenEventArgs e = new ListenEventArgs(endpoint, backlog);
            EventHandler<ListenEventArgs> h = Listening;
            if (h != null)
                h(this, e);

            //TODO - Remove this
            h = AfterListen;
            if (h != null)
                h(this, e);
        }
        
        public event EventHandler<AcceptEventArgs> ClientConnected;
        protected virtual void OnClientConnected(Socket socket, int connections)
        {
            AcceptEventArgs e = new AcceptEventArgs(socket, connections);
            EventHandler<AcceptEventArgs> h = ClientConnected;
            if (h != null)
                h(this, e);

            //TODO - Remove this
            h = Connected;
            if (h != null)
                h(this, e);
            
        }
        public event EventHandler<BroadcastEventArgs> BeforeBroadcast;
        protected virtual void OnBeforeBroadcast(byte[] data, int clients)
        {
            EventHandler<BroadcastEventArgs> h = BeforeBroadcast;
            if (h != null)
                h(this, new BroadcastEventArgs(data, clients));
        }
        public event EventHandler<BroadcastEventArgs> AfterBroadcast;
        protected virtual void OnAfterBroadcast(byte[] data, int clients)
        {
            EventHandler<BroadcastEventArgs> h = AfterBroadcast;
            if (h != null)
                h(this, new BroadcastEventArgs(data, clients));
        }
        public event EventHandler<ClientDisconnectEventArgs> ClientDisconnected;
        protected virtual void onClientDisconnect(IPEndPoint endpoint, int clients)
        {
            EventHandler<ClientDisconnectEventArgs> h = ClientDisconnected;
            if (h != null)
                h(this, new ClientDisconnectEventArgs(endpoint, clients));
        }
        #endregion

        public Server(string host, int port, AddressFamily addressFamily = AddressFamily.InterNetwork, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
            : base(host, port, addressFamily, socketType, protocol)
        {
            Initialize();
        }
        public Server(IPAddress host, int port, AddressFamily addressFamily = AddressFamily.InterNetwork, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
            : base(host, port, addressFamily, socketType, protocol)
        { }

        private void Initialize()
        {
            base.Disconnected += DisconnectedHandler;
            sockets = Hashtable.Synchronized(new Hashtable());
        }

        private void DisconnectedHandler(object sender, DisconnectEventArgs e)
        {
            
            Socket tmp = (Socket)sockets[e.getHandle().ToString()];
            IPEndPoint tmpIP = null;
            
            try
            {
                tmpIP = (IPEndPoint)tmp.RemoteEndPoint;
                tmp.Close();
            }
            catch (Exception) { }
            finally
            {
                lock(sockets.SyncRoot)
                {
                    sockets.Remove(e.getHandle().ToString());
                    onClientDisconnect(tmpIP, sockets.Count);
                }
            }
            
        }


        protected Thread listenThread;
        protected Thread pollingThread;
        protected ManualResetEvent manualResetEvent;
        protected Hashtable sockets;
        protected int backlog;
        ~Server()
        {
            isClosing = true;
            if (manualResetEvent != null)
                manualResetEvent.Set();
            if (listenThread != null)
                listenThread.Join();
            if (pollingThread != null)
            {
                pollingThread.Join();
            }
            if (sockets != null && sockets.Count > 0)
            {
                foreach (Socket socket in sockets)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                sockets.Clear();
            }
            sockets = null;
            listenThread = null;
            manualResetEvent = null;
        }
        protected void doPolling()
        {
            while (true)
            {
                if (isClosing)
                    return;
                lock (sockets.SyncRoot)
                {
                    Socket socket;
                    foreach (DictionaryEntry entry in sockets)
                    {
                        socket = (Socket)entry.Value;
                        try
                        {
                            if (!socket.Connected)
                                throw new SocketException();
                            socket.Poll(1, SelectMode.SelectRead);
                            
                        }
                        catch (SocketException)
                        {
                            sockets.Remove(entry.Key);
                            OnDisconnect(socket);
                        }
                        catch (Exception e)
                        {
                            onException(e);
                        }
                    }
                }
                Thread.Sleep(1000);
            }
            
        }
        protected void doListen()
        {
            //if (Thread.CurrentThread.Name != "Listening Thread")
            //    Thread.CurrentThread.Name = "Listening Thread";
            try
            {
                manualResetEvent = new ManualResetEvent(true);
                if (manualResetEvent == null)
                {
                    OnError("Could not create a ManualResetEvent");
                    return;
                }


                // Tell this socket where to listen from
                socket.Bind(EndPoint);

                OnBeforeListen((IPEndPoint)EndPoint, backlog);

                socket.Listen(backlog);

                OnListening((IPEndPoint)EndPoint, backlog);


                // Now run an I-Loop to continuously accept connections
                while (true)
                {
                    if (isClosing)
                        break;
                    manualResetEvent.Reset();

                    // Start the accept process
                    socket.BeginAccept(new AsyncCallback(AcceptCallback), null);

                    // Pause this thread
                    // If this fails, It is because the server is shutting down
                    manualResetEvent.WaitOne();
                }
            }
            catch (Exception e)
            {
                onException(e);
            }
        }
        public void Listen(int backlog = 0)
        {

            if (backlog != 0)
                this.backlog = backlog;

            //exception = null;

            listenThread = new Thread(doListen);
            pollingThread = new Thread(doPolling);

            try
            {
                listenThread.Start();
                pollingThread.Start();
            }
            catch (Exception e)
            {
                onException(e);
            }
        }
        public void Broadcast(string data)
        {
            Broadcast(Encoding.Unicode.GetBytes(data));
        }
        public void Broadcast(byte[] data)
        {
            if (sockets != null && sockets.Count > 0)
            {
                lock (sockets.SyncRoot)
                {
                    int sent = 0;
                    OnBeforeBroadcast(data, sockets.Count);
                    foreach (Socket socket in sockets.Values.Cast<Socket>())
                    {
                        try
                        {
                            Send(socket, data);

                            //lastDataSent = data;

                            sent++;
                        }
                        catch (Exception e)
                        {
                            onException(e);
                        }
                    }
                    OnAfterBroadcast(data, sent);
                }
            }
        }

        protected void AcceptCallback(IAsyncResult result)
        {

            //if (Thread.CurrentThread.Name != "Accepting Thread")
            //{
            //    Thread.CurrentThread.Name = "Accepting Thread";
            //}

            Socket socket;

            try
            {
                if (isClosing)
                {
                    return;
                }

                socket = this.socket.EndAccept(result);
                manualResetEvent.Set();
                IPEndPoint tmp = (IPEndPoint)socket.RemoteEndPoint;

                lock (sockets.SyncRoot)
                {
                    // loAcceptEventArgs.GetIpAddress & ":" & loAcceptEventArgs.GetPort
                    sockets.Add(socket.Handle.ToString(), socket);
                }

                OnClientConnected(socket, sockets.Count);

                State state = new State()
                {
                    Buffer = new byte[1024],
                    Socket = socket
                };

                state.Socket.BeginReceive(state.Buffer, 0, state.Length, SocketFlags.None, ReceiveCallback, state);
            }
            catch (Exception e)
            {
                onException(e);
            }
        }
        public new void Dispose(bool disposing)
        {
            if (manualResetEvent != null)
            {
                manualResetEvent.Set();
                manualResetEvent.Close();
            }
            manualResetEvent = null;
            base.Dispose(disposing);
        }
    }
}