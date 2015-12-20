namespace SchreckLib.Networking.Events
{
    using System.Net;
    public class ListenEventArgs : EndpointEventArgs
    {
        int backlog;
        public ListenEventArgs(IPEndPoint endpoint, int backlog) : base(endpoint) {
            this.backlog = backlog;
        }
        public int getBacklog()
        {
            return backlog;
        }
    }
}
