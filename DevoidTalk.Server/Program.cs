using System;
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using System.IO;

namespace DevoidTalk.Server
{
    static class Program
    {
        private sealed class Options
        {
            [Option('p', "port", DefaultValue = 1000, HelpText = "The network port for incoming connections.")]
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
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine("Port is {0}", options.Port);
            }
            else
            {
                // Display the default usage information
                Console.WriteLine(options.GetUsage());
            }
        }
    }
}
