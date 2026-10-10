using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SharpMonoInjector;

namespace AQTrainer
{
    internal static class Program
    {
        private const string AgentFile = "AQTAgent.dll";

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== All Quiet in the Trenches - Trainer (single player) ===");
            Console.WriteLine();

            int exitCode = Run(args);

            Console.WriteLine();
            Console.Write("Press any key to close...");
            try { Console.ReadKey(true); } catch (Exception) { }
            return exitCode;
        }

        private static int Run(string[] args)
        {
            try
            {
                string agentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AgentFile);
                if (!File.Exists(agentPath))
                {
                    Console.WriteLine("ERROR: cannot find " + AgentFile + " next to this program.");
                    Console.WriteLine("Path searched: " + agentPath);
                    return 2;
                }

                Process game = FindGame(args);
                if (game == null)
                {
                    Console.WriteLine("ERROR: cannot find the running game.");
                    Console.WriteLine("Start 'All Quiet in the Trenches' (reaching the main menu is enough) and run this program again.");
                    Console.WriteLine("If the process has a different name, pass it as an argument:  AQTrainer.exe <pid | name>");
                    return 3;
                }

                Console.WriteLine("Game found: " + game.ProcessName + " (PID " + game.Id + ")");
                byte[] bytes = File.ReadAllBytes(agentPath);
                Console.WriteLine("Injecting " + AgentFile + " (" + bytes.Length + " bytes)...");

                using (Injector injector = new Injector(game.Id))
                {
                    Console.WriteLine("64-bit process: " + injector.Is64Bit);
                    IntPtr asm = injector.Inject(bytes, "AQT", "Loader", "Load");
                    Console.WriteLine("Injected OK (assembly @ 0x" + asm.ToInt64().ToString("X") + ").");
                }

                Console.WriteLine();
                Console.WriteLine("Done. Go back to the game window (click it or Alt+Tab).");
                Console.WriteLine("The trainer menu appears on screen as soon as the game draws a frame.");
                Console.WriteLine("  - Show/hide menu: F10 or Insert");
                Console.WriteLine("  - Diagnostic log: " + Path.Combine(Path.GetTempPath(), "AQTrainer.log"));
                return 0;
            }
            catch (InjectorException e)
            {
                Console.WriteLine("INJECTION ERROR: " + e.Message);
                if (e.InnerException != null) Console.WriteLine("  Details: " + e.InnerException.Message);
                return 4;
            }
            catch (Exception e)
            {
                Console.WriteLine("UNEXPECTED ERROR: " + e);
                return 5;
            }
        }

        private static Process FindGame(string[] args)
        {
            Process[] all = Process.GetProcesses();

            if (args.Length > 0)
            {
                int pid;
                if (int.TryParse(args[0], out pid))
                {
                    for (int i = 0; i < all.Length; i++)
                        if (all[i].Id == pid) return all[i];
                    return null;
                }
                for (int i = 0; i < all.Length; i++)
                    if (all[i].ProcessName.IndexOf(args[0], StringComparison.OrdinalIgnoreCase) >= 0) return all[i];
                return null;
            }

            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].ProcessName.Replace(" ", "").Replace("_", "");
                if (n.IndexOf("AllQuiet", StringComparison.OrdinalIgnoreCase) >= 0) return all[i];
            }
            return null;
        }
    }
}
