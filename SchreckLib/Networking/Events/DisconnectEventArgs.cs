namespace SchreckLib.Networking.Events
{
    using System;
    using System.Net;
    public class DisconnectEventArgs : EventArgs
    {
        
        private IntPtr handle;
        public DisconnectEventArgs(IntPtr handle , int clients) {
            this.handle = handle;
        }
        public IntPtr getHandle()
        {
            return handle;
        }

    }
}