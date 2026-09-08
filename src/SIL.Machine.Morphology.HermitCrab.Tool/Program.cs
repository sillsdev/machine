using System;
using System.IO;
using System.Text;
using ManyConsole;
using ManyConsole.Internal;
using Mono.Options;
using SIL.Extensions;

namespace SIL.Machine.Morphology.HermitCrab;

internal class Program
{
    public static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.Unicode;
        Console.OutputEncoding = Encoding.Unicode;

        string inputFile = null;
        string outputFile = null;
        string scriptFile = null;
        bool showHelp = false;
        bool quitOnError = true;

        var p = new OptionSet
        {
            { "i|input-file=", "read configuration from {FILE}", value => inputFile = value },
            { "o|output-file=", "write results to {FILE}", value => outputFile = value },
            { "s|script-file=", "runs commands from {FILE}", value => scriptFile = value },
            {
                "c|continue",
                "continues when an error occurs while loading the configuration",
                value => quitOnError = value == null
            },
            { "h|help", "show this help message and exit", value => showHelp = value != null },
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

            Console.Write("Reading configuration file \"{0}\"... ", Path.GetFileName(inputFile));
            Language language = XmlLanguageLoader.Load(
                inputFile,
                quitOnError ? (Action<Exception, string>)null : (ex, id) => { }
            );
            Console.WriteLine("done.");

            context = new HCContext(language, output ?? Console.Out);
            Console.Write("Compiling rules... ");
            context.Compile();
            Console.WriteLine("done.");
            Console.WriteLine("{0} loaded.", language.Name);
            Console.WriteLine();
        }
        catch (IOException ioe)
        {
            Console.WriteLine();
            Console.WriteLine("IO Error: " + ioe.Message);
            output?.Close();
            return -1;
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine("Load Error: " + e.Message);
            output?.Close();
            return -1;
        }

        ConsoleCommand[] commands =
        {
            new ParseCommand(context),
            new TracingCommand(context),
            new TestCommand(context),
            new StatsCommand(context),
            new BatchCommand(context),
        };

        string input;
        int exitCode = 0;
        if (!string.IsNullOrEmpty(scriptFile))
        {
            using (var scriptReader = new StreamReader(scriptFile))
            {
                input = scriptReader.ReadLine();
                while (input != null)
                {
                    if (!input.Trim().StartsWith("#") && input.Trim() != "")
                    {
                        string[] cmdArgs = SplitCommandLine(input);
                        int result = ConsoleCommandDispatcher.DispatchCommand(commands, cmdArgs, context.Out);
                        // Remember the first nonzero result rather than the last: once a script
                        // line fails, later lines running (or not) shouldn't paper over that with
                        // a subsequent success.
                        if (result != 0 && exitCode == 0)
                            exitCode = result;
                    }
                    input = scriptReader.ReadLine();
                }
            }
        }
        else
        {
            Console.Write("> ");
            input = Console.ReadLine();
            while (input != null && input.Trim() != "exit")
            {
                if (input.Trim().IsOneOf("?", "help"))
                {
                    ConsoleHelp.ShowSummaryOfCommands(commands, Console.Out);
                }
                else
                {
                    string[] cmdArgs = SplitCommandLine(input);
                    ConsoleCommandDispatcher.DispatchCommand(commands, cmdArgs, context.Out);
                }
                Console.Write("> ");
                input = Console.ReadLine();
            }
        }

        output?.Close();

        return exitCode;
    }

    /// <summary>Splits a command line into tokens, honoring double- and single-quoted segments.
    /// Shared with <c>SIL.Machine.Morphology.HermitCrab.Conformance</c>'s <c>AdapterEngine</c> via
    /// <c>InternalsVisibleTo</c> so both consumers tokenize the exact same way instead of
    /// maintaining a second, divergent implementation (the Conformance harness's own copy did not
    /// handle single quotes).</summary>
    internal static string[] SplitCommandLine(string commandLine)
    {
        var parmChars = commandLine.ToCharArray();
        var inSingleQuote = false;
        var inDoubleQuote = false;
        for (var index = 0; index < parmChars.Length; index++)
        {
            if (parmChars[index] == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                parmChars[index] = '\n';
            }
            if (parmChars[index] == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                parmChars[index] = '\n';
            }
            if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                parmChars[index] = '\n';
        }
        return new string(parmChars).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static void ShowHelp(OptionSet p)
    {
        Console.WriteLine("Usage: hc [OPTIONS]");
        Console.WriteLine("HermitCrab.NET is a phonological and morphological parser.");
        Console.WriteLine();
        p.WriteOptionDescriptions(Console.Out);
    }
}
