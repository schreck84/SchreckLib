﻿namespace SchreckLib.Networking.Utils
{
    using System.Net.Sockets;
    public class State
    {
        public Socket Socket { get; set; }
        public byte[] Buffer { get; set; }
        public int Length
        {
            get
            {
                return Buffer.Length;
            }
        }
    }
}
