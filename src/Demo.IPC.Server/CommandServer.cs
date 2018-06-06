using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Demo.IPC.Server.Utils;
using Serilog;

namespace Demo.IPC.Server
{
    public class CommandServer
    {
        public CommandServer(string name, ILogger logger = null)
        {
            _name = name;
            _commands = new Dictionary<string, Func<string>>();
            _servers = new List<NamedPipeServerStream>();

            _log = logger ?? Log.Logger.ForContext<CommandServer>();

            RegisterDefaultCommands();
        }

        private readonly string _name;
        private readonly Dictionary<string, Func<string>> _commands;
        private readonly IList<NamedPipeServerStream> _servers;
        private readonly ILogger _log;

        private readonly string CommandNameCannotBeNull = "Command name cannot be null or blank";
        private readonly string CommandNameNotFound = "Command name not found";

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public void RegisterCommand(string name, Func<string> func)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _log.Error(CommandNameCannotBeNull);
                throw new ArgumentNullException("name", CommandNameCannotBeNull);
            }

            _commands.Add(name.ToLower(), func);
        }

        private string ExecuteCommand(string name)
        {
            _log.Debug("Execute command '{name}'", name ?? "null");

            if (string.IsNullOrWhiteSpace(name))
            {
                _log.Error(CommandNameCannotBeNull);
                return CommandNameCannotBeNull;
            }

            name = name?.ToLower();

            if (!_commands.ContainsKey(name))
            {
                _log.Error(CommandNameNotFound + ": {name}", name);
                return CommandNameNotFound;
            }

            try
            {
                return _commands[name].Invoke();
            }
            catch (Exception e)
            {
                _log.Error(e, "Error execute command '{name}': ", name, e.Message);
                return $"Error: {e.Message}";
            }
        }
        
        public async Task StartAsync(int capacity = 2)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(capacity);
            try
            {
                while (true)
                {
                    await semaphore.WaitAsync(CancellationToken);
                    var param = new PipeServerThreadParameter()
                    {
                        Semaphore = semaphore,
                        Id = GetRandomId()
                    };

                    var thread = new Thread(StartPipeServerThread);
                    thread.Name = $"Pipe {param.Id}";
                    thread.Start(param);
                    _log.Verbose("Pipe server ({id}) thread start", param.Id);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _log.Error(ex, "Error running named pipes");
                throw;
            }
            finally
            {
                semaphore.Dispose();
            }
        }
                
        private void StartPipeServerThread(object obj)
        {
            var param = obj as PipeServerThreadParameter;

            var server = new NamedPipeServerStream(
                _name,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);
            
            try
            {
                while (true)
                {
                    _log.Debug("Pipe server ({id}) waiting for connection", param.Id);
                    
                    server.WaitForConnectionAsync(CancellationToken).GetAwaiter().GetResult();

                    var ss = new StreamString(server);
                    var request = ss.ReadString();

                    var response = ExecuteCommand(request);

                    ss.WriteString(response);
                    server.WaitForPipeDrain();
                    server.Disconnect();
                }
            }
            catch (OperationCanceledException)
            {
                _log.Debug("Pipe server ({id}) shutting down", param.Id);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Pipe server ({id}) error: {message}", param.Id, ex.Message);

                if (param.Semaphore != null)
                {
                    param.Semaphore.Release();
                }
            }
            finally
            {
                server.Close();
                server.Dispose();
            }

            _log.Verbose("Pipe server ({id}) thread terminating", param.Id);
        }
        
        private string GetRandomId()
        {
            using (var provider = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                provider.GetBytes(bytes);
                var value = BitConverter.ToUInt32(bytes, 0);
                return Base62Convert.ToBase62String(value);
            }
        }

        private class PipeServerThreadParameter
        {
            public SemaphoreSlim Semaphore { get; set; }
            public string Id { get; set; }
        }
        
        private void RegisterDefaultCommands()
        {
            RegisterCommand("file", () => Commands.FileCommand());
            RegisterCommand("echo", () => Commands.EchoCommand());
            RegisterCommand("status", () => Commands.StatusCommand());
            RegisterCommand("thread", () => Commands.ThreadCommand());
        }
    }
}
