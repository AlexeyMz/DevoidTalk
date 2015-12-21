using DevoidTalk.Core;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevoidTalk.Server
{
    public sealed class BroadcastingChat
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly ConnectionManager connectionManager;
        readonly string welcomeMessage;

        public BroadcastingChat(ConnectionManager connectionManager, string welcomeMessage)
        {
            this.connectionManager = connectionManager;
            this.welcomeMessage = welcomeMessage;

            connectionManager.ClientConnected += ClientConnected;
            connectionManager.ClientDisconnected += ClientDisconnected;
            connectionManager.IncomingMessage += IncomingMessage;
        }

        private async void ClientConnected(object sender, ClientConnection connection)
        {
            try
            {
                await BroadcastToAll(new Message { Sender = "<server>", Text = $"{connection} connected" });
                await ReplyTo(connection, new Message { Sender = "<server>", Text = welcomeMessage });
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} connected handling", connection);
            }
        }

        private async void ClientDisconnected(object sender, ClientConnection connection)
        {
            try
            {
                await BroadcastToAll(new Message { Sender = "<server>", Text = $"{connection} disconnected" });
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} disconnected handling", connection);
            }
        }

        private async void IncomingMessage(object sender, IncomingMessage e)
        {
            try
            {
                var message = e.Message.Text.TrimStart();
                if (message.StartsWith("/"))
                {
                    await ReplyTo(e.Sender, new Message { Sender = "<server>", Text = "Commands are not supported yet :'(" });
                }
                else
                {
                    await BroadcastToAll(e.Message);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Incoming message from {0} handling", e.Sender);
            }
        }

        private async Task BroadcastToAll(Message message)
        {
            await Task.Yield();
            var tasks = from client in connectionManager.Clients
                        select client.SendMessage(message);
            await Task.WhenAll(tasks);
        }

        private async Task ReplyTo(ClientConnection client, Message message)
        {
            await Task.Yield();
            await client.SendMessage(message);
        }
    }
}
