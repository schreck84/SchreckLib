namespace SchreckLib.Networking.Events
{
    using System;
    public class ExceptionEventArgs : EventArgs
    {
        Exception exception;
        public ExceptionEventArgs(Exception e)
        {
            exception = e;
        }
        public Exception getException()
        {
            return exception;
        }
    }
}
