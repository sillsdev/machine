using System.Collections.Generic;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;

namespace SIL.Machine;

public class PreprocessCommandSpec : ICommandSpec
{
    private const string Nfc = "nfc";
    private const string Nfd = "nfd";
    private const string Nfkc = "nfkc";
    private const string Nfkd = "nfkd";

    private CommandOption _lowercaseOption;
    private CommandOption _normalizeOption;

    public bool EscapeSpaces { get; set; } = false;

    public void AddParameters(CommandBase command)
    {
        _lowercaseOption = command.Option("-l|--lowercase", "Convert text to lowercase.", CommandOptionType.NoValue);
        _normalizeOption = command.Option(
            "-nf|--normalization-form <FORM>",
            $"Normalizes text to the specified form.\nForms: \"{Nfc}\", \"{Nfd}\", \"{Nfkc}\", \"{Nfkd}\".",
            CommandOptionType.SingleValue
        );
    }

    public bool Validate(TextWriter outWriter)
    {
        if (!ValidateNormalizeOption(_normalizeOption.Value()))
        {
            outWriter.WriteLine("The specified normalization form is invalid.");
            return false;
        }

        return true;
    }

    public IParallelTextCorpus Preprocess(IParallelTextCorpus corpus)
    {
        switch (_normalizeOption.Value())
        {
            case Nfc:
                corpus = corpus.NfcNormalize();
                break;
            case Nfd:
                corpus = corpus.NfdNormalize();
                break;
            case Nfkc:
                corpus = corpus.NfkcNormalize();
                break;
            case Nfkd:
                corpus = corpus.NfkdNormalize();
                break;
        }

        if (EscapeSpaces)
            corpus = corpus.EscapeSpaces();

        if (_lowercaseOption.HasValue())
            corpus = corpus.Lowercase();

        return corpus;
    }

    public ITextCorpus Preprocess(ITextCorpus corpus)
    {
        switch (_normalizeOption.Value())
        {
            case Nfc:
                corpus = corpus.NfcNormalize();
                break;
            case Nfd:
                corpus = corpus.NfdNormalize();
                break;
            case Nfkc:
                corpus = corpus.NfkcNormalize();
                break;
            case Nfkd:
                corpus = corpus.NfkdNormalize();
                break;
        }

        if (EscapeSpaces)
            corpus = corpus.EscapeSpaces();

        if (_lowercaseOption.HasValue())
            corpus = corpus.Lowercase();

        return corpus;
    }

    private static bool ValidateNormalizeOption(string value)
    {
        var validForms = new HashSet<string> { Nfc, Nfd, Nfkc, Nfkd };
        return string.IsNullOrEmpty(value) || validForms.Contains(value.ToLowerInvariant());
    }
}
