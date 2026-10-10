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
            Console.WriteLine("=== All Quiet in the Trenches - Trainer (un jugador) ===");
            Console.WriteLine();

            int exitCode = Run(args);

            Console.WriteLine();
            Console.Write("Presiona una tecla para cerrar...");
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
                    Console.WriteLine("ERROR: no encuentro " + AgentFile + " junto a este programa.");
                    Console.WriteLine("Ruta buscada: " + agentPath);
                    return 2;
                }

                Process game = FindGame(args);
                if (game == null)
                {
                    Console.WriteLine("ERROR: no encuentro el juego en ejecucion.");
                    Console.WriteLine("Abre 'All Quiet in the Trenches' (idealmente hasta el menu principal) y vuelve a ejecutar este programa.");
                    Console.WriteLine("Si el proceso tiene otro nombre, pasalo como argumento:  AQTrainer.exe <pid | nombre>");
                    return 3;
                }

                Console.WriteLine("Juego encontrado: " + game.ProcessName + " (PID " + game.Id + ")");
                byte[] bytes = File.ReadAllBytes(agentPath);
                Console.WriteLine("Inyectando " + AgentFile + " (" + bytes.Length + " bytes)...");

                using (Injector injector = new Injector(game.Id))
                {
                    Console.WriteLine("Proceso de 64 bits: " + injector.Is64Bit);
                    IntPtr asm = injector.Inject(bytes, "AQT", "Loader", "Load");
                    Console.WriteLine("Inyectado OK (assembly @ 0x" + asm.ToInt64().ToString("X") + ").");
                }

                Console.WriteLine();
                Console.WriteLine("Listo. Vuelve a la ventana del juego (haz clic en ella o Alt+Tab).");
                Console.WriteLine("El menu del trainer aparece en pantalla en cuanto el juego dibuje un frame.");
                Console.WriteLine("  - Mostrar/ocultar menu: F10 o Insert");
                Console.WriteLine("  - Log de diagnostico:   " + Path.Combine(Path.GetTempPath(), "AQTrainer.log"));
                return 0;
            }
            catch (InjectorException e)
            {
                Console.WriteLine("ERROR de inyeccion: " + e.Message);
                if (e.InnerException != null) Console.WriteLine("  Detalle: " + e.InnerException.Message);
                return 4;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR inesperado: " + e);
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
