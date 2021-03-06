﻿using DevoidTalk.Core;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace DevoidTalk.Server
{
    public sealed class ConnectionManager
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly IClientAcceptor acceptor;
        readonly CancellationToken cancellation;

        IImmutableSet<ClientConnection> clients = ImmutableHashSet<ClientConnection>.Empty;

        public event EventHandler<ClientConnection> ClientConnected;
        public event EventHandler<ClientConnection> ClientDisconnected;
        public event EventHandler<IncomingMessage> IncomingMessage;

        public IImmutableSet<ClientConnection> Clients
        {
            get { return clients; }
        }

        public ConnectionManager(IClientAcceptor acceptor, CancellationToken cancellation)
        {
            this.acceptor = acceptor;
            this.cancellation = cancellation;
            acceptor.ClientAccepted += OnClientAccepted;
        }

        private async void OnClientAccepted(object sender, ClientConnection connection)
        {
            ImmutableInterlocked.Update(ref clients, oldClients => oldClients.Add(connection));
            logger.Debug("{0} connected", connection);
            OnClientConnected(connection);

            int? socketErrorCode = null;

            try
            {
                await ReadClientMessages(connection);
            }
            catch (Exception ex)
            {
                if (ex is DisconnectedException && ex.InnerException != null)
                {
                    var socketException = ex.InnerException as SocketException;
                    if (socketException != null)
                        socketErrorCode = socketException.ErrorCode;
                }

                if (ex is OperationCanceledException || ex is DisconnectedException) {}
                else { logger.Warn(ex, "{0} disconnected with error", connection); }
            }
            finally
            {
                ImmutableInterlocked.Update(ref clients, oldClients => oldClients.Remove(connection));

                if (socketErrorCode.HasValue)
                    logger.Debug("{0} disconnected ({1}: {2})", connection, socketErrorCode, (SocketError)socketErrorCode);
                else
                    logger.Debug("{0} disconnected", connection);
                
                OnClientDisconnected(connection);
            }
        }

        private async Task ReadClientMessages(ClientConnection connection)
        {
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                var message = await connection.ReadMessage();
                connection.LastUsername = message.Sender;
                OnIncomingMessage(new IncomingMessage(connection, message));
            }
        }

        private void OnClientConnected(ClientConnection connection)
        {
            ClientConnected?.Invoke(this, connection);
        }

        private void OnClientDisconnected(ClientConnection connection)
        {
            ClientDisconnected?.Invoke(this, connection);
        }

        private void OnIncomingMessage(IncomingMessage incomingMessage)
        {
            IncomingMessage?.Invoke(this, incomingMessage);
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
