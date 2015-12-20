namespace SchreckLib.Networking.Events
{
    using System;
    public class ErrorEventArgs : EventArgs
    {
        string error;
        public ErrorEventArgs(string error)
        {
            this.error = error;
        }
        public string getError()
        {
            return error;
        }
    }
}
