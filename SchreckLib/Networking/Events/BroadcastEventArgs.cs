namespace SchreckLib.Networking.Events
{
    using System;
    using System.Text;
    public class BroadcastEventArgs : EventArgs
    {
        protected byte[] data;
        protected int clients;
        public BroadcastEventArgs(byte[] data, int clients)
        {
            this.clients = clients;
            this.data = data;
        }
        public byte[] getBytes()
        {
            return data;
        }
        public string getText()
        {
            return Encoding.Unicode.GetString(data);
        }
        public int getClientCount()
        {
            return clients;
        }
    }
}
