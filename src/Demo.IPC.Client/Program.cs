using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Demo.IPC.Server;

#if Net
using System.Runtime.InteropServices;
#endif

namespace Demo.IPC.Client
{
    class Program
    {
        public static void Main(string[] args)
        {
            var cmd = ConfigureCommandLine();
            try
            {
                Environment.ExitCode = cmd.Execute(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = -1;
            }
        }

        private static CommandLineApplication ConfigureCommandLine()
        {
            var app = new CommandLineApplication(false);
            app.HelpOption("-h");

#if Net
            var list = app.Option(
                "--list|-l",
                "list named pipes",
                CommandOptionType.SingleValue);
#endif

            var name = app.Option(
                "--name|-n",
                "pipe name",
                CommandOptionType.SingleValue);

            var command = app.Option(
                "--command|-c",
                "server command",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
#if Net
                if (list.HasValue())
                {
                    return ListNamedPipes(list.Value());
                }
#endif
                if (name.HasValue() && command.HasValue())
                {
                    return ConnectToPipeServer(name.Value(), command.Value());
                }

                var message = "One or more parameters are incorrect or missing";
                Console.Error.WriteLine(message);
                app.ShowHelp();
                return -1;
            });

            return app;
        }

        private static int ConnectToPipeServer(string name, string command)
        {
            var result = 0;

            var pipeClient = new NamedPipeClientStream(
                ".",
                name,
                PipeDirection.InOut,
                PipeOptions.None,
                TokenImpersonationLevel.Impersonation);

            try
            {
                pipeClient.Connect(10000);
                StreamString ss = new StreamString(pipeClient);
                ss.WriteString(command);
                Console.Out.WriteLine(ss.ReadString());
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                result = - 1;
            }
            finally
            {
                pipeClient.Close();
            }
            return result;
        }

#if Net
        private static int ListNamedPipes(string name)
        {
            try
            {
                foreach (var item in WindowsNamedPipeUtil.NamedPipes())
                {
                    if (!string.IsNullOrEmpty(item) && item.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Out.WriteLine(item);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }
#endif
    }
}
