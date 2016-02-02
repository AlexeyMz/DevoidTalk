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
            serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
        }

        public async Task Listen(CancellationToken cancellation)
        {
            serverSocket.Listen(100);
            var cancellationTcs = new TaskCompletionSource<bool>();
            cancellation.Register(() => cancellationTcs.TrySetCanceled());

            while (true)
            {
                cancellation.ThrowIfCancellationRequested();

                var acceptTask = Task.Factory.FromAsync(
                    serverSocket.BeginAccept, serverSocket.EndAccept, null);

                await Task.WhenAny(cancellationTcs.Task, acceptTask);
                cancellation.ThrowIfCancellationRequested();

                var clientSocket = await acceptTask;
                OnClientAccepted(clientSocket);
            }
        }

        private void OnClientAccepted(Socket clientSocket)
        {
            var clientConnection = new ClientConnection(clientSocket);
            ClientAccepted?.Invoke(this, clientConnection);
        }
    }
}
