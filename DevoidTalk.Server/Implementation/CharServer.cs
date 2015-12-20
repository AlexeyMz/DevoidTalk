using DevoidTalk.Core;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevoidTalk.Server.Implementation
{
    public sealed class CharServer
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly IClientAcceptor acceptor;
        readonly Core.ThreadPool threadPool;
        readonly CancellationToken cancellation;

        IImmutableSet<ClientConnection> clients = ImmutableHashSet<ClientConnection>.Empty;

        public event EventHandler<ClientConnection> ClientConnected;
        public event EventHandler<ClientConnection> ClientDisconnected;
        public event EventHandler<IncomingMessage> IncomingMessage;

        public IImmutableSet<ClientConnection> Clients
        {
            get { return clients; }
        }

        public CharServer(
            IClientAcceptor acceptor,
            Core.ThreadPool threadPool,
            CancellationToken cancellation)
        {
            acceptor.ClientAccepted += OnClientAccepted;
        }

        private async void OnClientAccepted(object sender, ClientConnection connection)
        {
            ImmutableInterlocked.Update(ref clients, oldClients => oldClients.Add(connection));
            logger.Debug("{0} connected", connection);

            try
            {
                await ReadClientMessages(connection);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) { }
                else { logger.Warn(ex, "{0} disconnected with error", connection); }
            }
            finally
            {
                ImmutableInterlocked.Update(ref clients, oldClients => oldClients.Remove(connection));
                logger.Debug("{0} disconnected");
            }
        }

        private async Task ReadClientMessages(ClientConnection connection)
        {
            while (!cancellation.IsCancellationRequested)
            {
                var message = await connection.ReadMessage();
                OnIncomingMessage(new IncomingMessage(connection, message));
            }
        }

        private void OnIncomingMessage(IncomingMessage incomingMessage)
        {
            var handlers = IncomingMessage;
            if (handlers != null)
                handlers(this, incomingMessage);
        }
    }

    public struct IncomingMessage
    {
        public ClientConnection Sender { get; }
        public Message Message { get; }
        public IncomingMessage(ClientConnection sender, Message message)
        {
            Sender = sender;
            Message = message;
        }
    }
}
