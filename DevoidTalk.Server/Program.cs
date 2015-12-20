using System;
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using System.IO;
using System.Threading;
using DevoidTalk.Core;

namespace DevoidTalk.Server
{
    static class Program
    {
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
            var cancellationSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationSource.Cancel();
            };

            IClientAcceptor acceptor = new TcpClientAcceptor(port);

            var threadPool = new Core.ThreadPool(10, cancellationSource.Token);

            threadPool.Post(async () =>
            {
                await acceptor.Listen(cancellationSource.Token);
                Console.WriteLine("Finished accepting");
            });

            while (!cancellationSource.IsCancellationRequested)
            {
                var command = Console.ReadLine();
            }
        }
    }
}
