using System;
using System.IO;
using NDesk.Options;
using ManyConsole;
using ManyConsole.Internal;
using SIL.Collections;

namespace SIL.HermitCrab
{
    public class HermitCrabProgram
    {
        public static int Main(string[] args)
        {
            string inputFile = null;
            string outputFile = null;
            bool showHelp = false;
            bool quitOnError = true;

			var p = new OptionSet
                    	{
                    		{ "i|input-file=", "read configuration from {FILE}", value => inputFile = value },
							{ "o|output-file=", "write results to {FILE}", value => outputFile = value },
							{ "c|continue", "continues when an error occurs", value => quitOnError = value == null },
							{ "h|help", "show this help message and exit", value => showHelp = value != null }
                    	};

            try
            {
                p.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(p);
                return -1;
            }

            if (showHelp || string.IsNullOrEmpty(inputFile))
            {
                ShowHelp(p);
                return -1;
            }

        	HCContext context;
        	TextWriter output = null;
            try
            {
				if (!string.IsNullOrEmpty(outputFile))
					output = new StreamWriter(outputFile);

				Console.Write("Reading configuration file...");
            	Language language = XmlLoader.Load(inputFile, quitOnError);
				Console.WriteLine("done.");

				context = new HCContext(language, output ?? Console.Out);
				Console.Write("Compiling rules...");
				context.Compile();
				Console.WriteLine("done.");
				Console.WriteLine("{0} loaded.", language.Description);
            }
            catch (IOException ioe)
            {
				Console.WriteLine();
                Console.WriteLine("IO Error: " + ioe.Message);
            	if (output != null)
					output.Close();
            	return -1;
            }
            catch (LoadException le)
            {
				Console.WriteLine();
                Console.WriteLine("Load Error: " + le.Message);
                if (le.InnerException != null)
                    Console.WriteLine(le.InnerException.Message);
            	if (output != null)
					output.Close();
            	return -1;
            }

			ConsoleCommand[] commands = { new ParseCommand(context), new TracingCommand(context) };

			Console.Write("> ");
        	string input = Console.ReadLine();
			while (input != null && input.Trim() != "exit")
			{
				if (input.Trim().IsOneOf("?", "help"))
				{
					ConsoleHelp.ShowSummaryOfCommands(commands, Console.Out);
				}
				else
				{
					string[] cmdArgs = input.ToCommandLineArgs();
					ConsoleCommandDispatcher.DispatchCommand(commands, cmdArgs, Console.Out);
				}
				Console.Write("> ");
				input = Console.ReadLine();
			}

			if (output != null)
				output.Close();

        	return 0;
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: hc [OPTIONS]");
            Console.WriteLine("HermitCrab.NET is a phonological and morphological parser.");
            Console.WriteLine();
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
