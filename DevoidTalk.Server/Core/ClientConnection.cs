using DevoidTalk.Core;
using System;
using System.Net;
using System.Net.Sockets;

namespace DevoidTalk.Server
{
    public sealed class ClientConnection : Connection
    {
        string lastUsername;

        public IPEndPoint EndPoint { get; }

        public string LastUsername
        {
            get { lock (this) { return lastUsername; } }
            set
            {
                if (value == lastUsername)
                    return;
                lock (this) { lastUsername = value; }
            }
        }

        public ClientConnection(Socket socket)
            : base(socket)
        {
            lock (this)
            {
                EndPoint = socket.RemoteEndPoint as IPEndPoint;
            }
        }

        public override string ToString()
        {
            string username = LastUsername;
            string clientName = username == null ? "client" : $"'{username}'";
            string endPoint = EndPoint == null
                ? "<unknown>" : EndPoint.ToString();
            return $"{clientName}@{endPoint}";
        }
    }
}

