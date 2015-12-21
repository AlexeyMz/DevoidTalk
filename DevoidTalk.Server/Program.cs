using System;
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using System.IO;
using System.Threading;
using DevoidTalk.Core;
using NLog;
using System.Threading.Tasks;

namespace DevoidTalk.Server
{
    static class Program
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private sealed class Options
        {
            [Option('p', "port", DefaultValue = 10000, HelpText = "The network port for incoming connections.")]
            public int Port { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                var serverAssembly = typeof(Program).Assembly;
                var assemblyName = serverAssembly.GetName();
                var title = serverAssembly.GetCustomAttribute<AssemblyTitleAttribute>();
                var company = serverAssembly.GetCustomAttribute<AssemblyCompanyAttribute>();

                var help = new HelpText {
                    Heading = new HeadingInfo(title.Title, assemblyName.Version.ToString()),
                    Copyright = new CopyrightInfo(company.Company, 2015),
                    AdditionalNewLineAfterOption = true,
                    AddDashesToOption = true
                };
                var assemblyExecutableName = Path.GetFileNameWithoutExtension(serverAssembly.Location);
                help.AddPreOptionsLine(string.Format("Usage: {0} -p 10000", assemblyExecutableName));
                help.AddOptions(this);
                return help;
            }
        }

        public static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                StartServer(options.Port);
            }
            else
            {
                // Display the default usage information
                Console.WriteLine(options.GetUsage());
            }
        }

        private static void StartServer(int port)
        {
            logger.Info("==========================");
            logger.Info("Starting server...");

            var cancellationSource = new CancellationTokenSource();
            var cancellation = cancellationSource.Token;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                logger.Info("Gracefully stopping server...");
                cancellationSource.Cancel();
            };

            var threadPool = new Core.ThreadPool(10, cancellation);
            IClientAcceptor acceptor = new TcpClientAcceptor(port);
            var connectionManager = new ConnectionManager(acceptor, threadPool, cancellation);
            var broadcastingChat = new BroadcastingChat(connectionManager);

            var tcs = new TaskCompletionSource<bool>();
            threadPool.Post(async () =>
            {
                logger.Info($"Start accepting clients at port {port}");
                try
                {
                    await acceptor.Listen(cancellation);
                    logger.Info("Finish accepting clients");
                    tcs.TrySetResult(true);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error accepting clients");
                    tcs.SetException(ex);
                }
            });

            try
            {
                tcs.Task.Wait();
            }
            catch (AggregateException ex)
            {
                try { ex.Handle(exc => exc is OperationCanceledException); }
                catch (Exception innerEx) { logger.Error(innerEx); }
            }
            logger.Info("Server stopped");
        }
    }
}
