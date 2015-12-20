using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace DevoidTalk.Server
{
    public sealed class TcpClientAcceptor : IClientAcceptor
    {
        Socket serverSocket;

        public event EventHandler<ClientConnection> ClientAccepted;

        public TcpClientAcceptor(int port)
        {
            serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public async Task Listen(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                var acceptTask = Task.Factory.FromAsync(
                    serverSocket.BeginAccept, serverSocket.EndAccept, null);

                var clientSocket = await acceptTask;
                OnClientAccepted(clientSocket);
            }
        }

        private void OnClientAccepted(Socket clientSocket)
        {
            var clientConnection = new ClientConnection(clientSocket);

            var clientAcceptedHandlers = ClientAccepted;
            if (clientAcceptedHandlers != null)
                clientAcceptedHandlers(this, clientConnection);
        }
    }
}
