namespace SchreckLib.Networking.Events
{
    using System;
    using System.Net;
    public abstract class EndpointEventArgs : EventArgs
    {
        private string ipAddress;
        private int port;
        public EndpointEventArgs(IPEndPoint endPoint)
        {
            ipAddress = endPoint.Address.ToString();
            port = endPoint.Port;
        }
        public String getAddress()
        {
            return ipAddress;
        }
        public int getPort()
        {
            return port;
        }
    }
}
