using System;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (!args.Contains("--serve"))
            {
                Console.Error.WriteLine(
                    "HermitCrab parser worker. Normally launched by HermitCrabServerClient.\n"
                        + "Usage: --serve --config <hc-config.xml> [--max-dop N]"
                );
                return 1;
            }

            string? configPath = GetArg(args, "--config");
            if (configPath == null)
            {
                Console.Error.WriteLine("Missing --config <hc-config.xml>");
                return 1;
            }

            int maxDop = int.TryParse(GetArg(args, "--max-dop"), out int dop) ? dop : 0;
            HermitCrabServerHost.Run(configPath, maxDop);
            return 0;
        }

        private static string? GetArg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            if (i >= 0 && i + 1 < args.Length)
                return args[i + 1];
            return null;
        }
    }
}
