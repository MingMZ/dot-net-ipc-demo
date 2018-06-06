using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Demo.IPC.Server
{
    public static class Commands
    {
        private static Serilog.ILogger Log = Serilog.Log.Logger.ForContext<CommandServer>();

        public static string EchoCommand()
        {
            Log.Verbose("{name} command", "echo");

            return "Echo";
        }

        public static string FileCommand()
        {
            Log.Verbose("{name} command", "file");

            var sb = new StringBuilder();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                if (!asm.GlobalAssemblyCache)
                {
                    var uri = new Uri(asm.CodeBase);
                    sb.AppendLine($"{asm.FullName} ({uri.AbsolutePath})");
                }
            }
            return sb.ToString();
        }

        public static string StatusCommand()
        {
            Log.Verbose("{name} command", "status");

            var sb = new StringBuilder();
            sb.AppendLine($"PeakWorkingSet: {Process.GetCurrentProcess().WorkingSet64.ToString("#,##0")}");
            sb.AppendLine($"TotalProcessorTime: {Process.GetCurrentProcess().TotalProcessorTime}");
            return sb.ToString();
        }

        public static string ThreadCommand()
        {
            Log.Verbose("{name} command", "thread");

            var sb = new StringBuilder();
            sb.AppendLine($"Threads: {Process.GetCurrentProcess().Threads.Count}");
            foreach (ProcessThread t in Process.GetCurrentProcess().Threads)
            {
                var waitReason = t.ThreadState == System.Diagnostics.ThreadState.Wait ? t.WaitReason.ToString() : String.Empty;
                sb.AppendLine($"- Id:{t.Id}, TotalProcessorTime:{t.TotalProcessorTime}, State:{t.ThreadState}, WaitReason:{waitReason}");
            }
            return sb.ToString();
        }
    }
}
