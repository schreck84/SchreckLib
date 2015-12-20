namespace SchreckLib.Networking.Events
{
    using System;
    using System.Net;
    public class ClientDisconnectEventArgs : EndpointEventArgs
    {
        protected int clients;
        public ClientDisconnectEventArgs(IPEndPoint endpoint, int clients) : base(endpoint)
        {
            this.clients = clients;
        }
        public int getClientCount()
        {
            return clients;
        }
        
    }
}
