using DevoidTalk.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DevoidTalk.Client
{
    public partial class MainForm : Form
    {
        readonly Regex serverAddressRegex = new Regex("^(?<host>[^:]+)(:(?<port>[0-9]+))?$");
        
        ConnectionDialog connectionDialog;

        bool triedToConnect = false;
        CancellationTokenSource cancellationSource;
        Connection connection;

        public MainForm()
        {
            InitializeComponent();

            var settings = Properties.Settings.Default;

            connectionDialog = new ConnectionDialog()
            {
                ServerAddress = settings.ServerAddress,
                ChatName = settings.ChatName,
            };
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            ShowConnectionDialog();
        }

        private void ShowConnectionDialog()
        {
            if (connectionDialog.ShowDialog(this) == DialogResult.OK)
            {
                TryConnect();
            }
            else
            {
                SaveSettings();
                Close();
            }
        }

        private async void TryConnect()
        {
            try
            {
                var connectTask = Connect();

                var result = new ConnectingProgress(connectTask).ShowDialog(this);
                if (result == DialogResult.Cancel)
                    throw new OperationCanceledException();

                cancellationSource = new CancellationTokenSource();

                connection = await connectTask;
                try
                {
                    cancellationSource.Token.Register(() => { var ignored = connection.Disconnect(); });
                    await ReadMessages(connection, cancellationSource.Token);
                }
                finally
                {
                    var ignore = connection.Disconnect();
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    var errorDialog = new ErrorDialog() { ErrorMessage = ex.ToString() };
                    errorDialog.ShowDialog(this);
                }
                ShowConnectionDialog();
            }
            
        }

        private async Task<Connection> Connect()
        {
            triedToConnect = true;

            Match addressMatch = serverAddressRegex.Match(connectionDialog.ServerAddress);
            if (!addressMatch.Success)
                throw new ArgumentException("Invalid server address (must be in form <host>[:<port>])");

            string host = addressMatch.Groups["host"].Value;

            var portGroup = addressMatch.Groups["port"];
            int port = 10000;
            if (portGroup.Success && !int.TryParse(portGroup.Value, out port))
                throw new ArgumentException($"Invalid server port ${portGroup.Value}");

            IPAddress[] hostAddresses = await Dns.GetHostAddressesAsync(host);
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectTaskAsync(hostAddresses, port);

            return new Connection(socket);
        }

        private async Task ReadMessages(Connection connection, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                var message = await connection.ReadMessage();
                PrintMessage(message);
            }
        }

        private void PrintMessage(Core.Message message)
        {
            chatBox.AppendText(Environment.NewLine);

            int offset = chatBox.TextLength;
            chatBox.AppendText(message.Sender + ":");
            chatBox.Select(offset, chatBox.TextLength - offset);
            chatBox.SelectionColor = Color.Blue;

            offset = chatBox.TextLength;
            chatBox.AppendText(" " + message.Text);
            chatBox.Select(offset, chatBox.TextLength - offset);
            chatBox.SelectionColor = Color.Black;

            chatBox.Select(chatBox.TextLength, 0);
            chatBox.ScrollToCaret();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            var ignored = connection.SendMessage(new Core.Message()
            {
                Sender = connectionDialog.ChatName,
                Text = messageBox.Text,
            });
            messageBox.Clear();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            cancellationSource?.Cancel();
            SaveSettings();
        }

        private void SaveSettings()
        {
            var settings = Properties.Settings.Default;
            if (triedToConnect)
            {
                settings.ServerAddress = connectionDialog.ServerAddress;
                settings.ChatName = connectionDialog.ChatName;
            }
            settings.Save();
        }
    }
}
