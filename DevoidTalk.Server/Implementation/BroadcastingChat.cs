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

        public BroadcastingChat(ConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;

            connectionManager.ClientConnected += ClientConnected;
            connectionManager.ClientDisconnected += ClientDisconnected;
            connectionManager.IncomingMessage += IncomingMessage;
        }

        private void ClientConnected(object sender, ClientConnection connection)
        {
            LogBroadcasting(BroadcastToAll(
                new Message { Sender = "<server>", Text = $"{connection} connected" }));
        }

        private void ClientDisconnected(object sender, ClientConnection connection)
        {
            LogBroadcasting(BroadcastToAll(
                new Message { Sender = "<server>", Text = $"{connection} disconnected" }));
        }

        private void IncomingMessage(object sender, IncomingMessage e)
        {
            LogBroadcasting(BroadcastToAll(e.Message));
        }

        private async Task BroadcastToAll(Message message)
        {
            await Task.Yield();
            var tasks = from client in connectionManager.Clients
                        select client.SendMessage(message);
            await Task.WhenAll(tasks);
        }

        private async void LogBroadcasting(Task broadcastingTask)
        {
            try
            {
                await broadcastingTask;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Broadcasting error");
            }
        }
    }
}
