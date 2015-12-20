namespace SchreckLib.Networking
{
    using Events;
    using Exceptions;
    using Networking.Data;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    class Client : Common
    {
        public event EventArgs<ConnectEventArgs> 
        protected virtual void onConnect()
        {

        }
        
        
    }
}
