using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Demo.IPC.Server;

namespace Demo.IPC.Tophelf
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            Console.CancelKeyPress += Console_CancelKeyPress;
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration) 
                .CreateLogger();
            
            var id = $"ipc.demo.{Guid.NewGuid().ToString()}";
            
            Log.Logger.Information("Server name: {id}", id);

            var server = new CommandServer(id);
            server.CancellationToken = CancellationTokenSource.Token;
            Log.Logger.Verbose("Total threads: {threads}", Process.GetCurrentProcess().Threads.Count);

            server.StartAsync().ContinueWith(t =>
                {
                    Console.WriteLine("Server shutting down");
                }, TaskContinuationOptions.OnlyOnCanceled)
                .Wait();
            
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
            Log.CloseAndFlush();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                Log.Debug("Ctrl+C pressed, abort connection...");
                CancellationTokenSource.Cancel();
                e.Cancel = true;
            }
        }

        public static IConfigurationRoot Configuration { get; private set; }
        public static CancellationTokenSource CancellationTokenSource { get; private set; } = new CancellationTokenSource();
        
    }
}
