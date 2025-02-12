using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Scripture;

namespace SIL.Machine;

internal static class ToolHelpers
{
    public static bool ValidateCorpusFormatOption(string value)
    {
        return string.IsNullOrEmpty(value) || value.ToLowerInvariant().IsOneOf("dbl", "usx", "text", "pt", "pt_m");
    }

    public static bool ValidateWordTokenizerOption(string value, bool supportsNullTokenizer = false)
    {
        var types = new HashSet<string> { "latin", "whitespace", "zwsp" };
        if (supportsNullTokenizer)
            types.Add("none");
        return string.IsNullOrEmpty(value) || types.Contains(value.ToLowerInvariant());
    }

    public static ITextCorpus CreateTextCorpus(string type, string path)
    {
        switch (type.ToLowerInvariant())
        {
            case "dbl":
                return new DblBundleTextCorpus(path);

            case "usx":
                return new UsxFileTextCorpus(path);

            case "pt":
                return new ParatextTextCorpus(path);

            case "text":
                return new TextFileTextCorpus(path);

            case "pt_m":
                return new ParatextTextCorpus(path, includeMarkers: true);
        }

        throw new ArgumentException("An invalid text corpus type was specified.", nameof(type));
    }

    public static IAlignmentCorpus CreateAlignmentsCorpus(string type, string path)
    {
        switch (type.ToLowerInvariant())
        {
            case "text":
                return new TextFileAlignmentCorpus(path);
        }

        throw new ArgumentException("An invalid alignment corpus type was specified.", nameof(type));
    }

    public static IRangeTokenizer<string, int, string> CreateWordTokenizer(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "latin":
                return new LatinWordTokenizer();

            case "none":
                return new NullTokenizer();

            case "zwsp":
                return new ZwspWordTokenizer();

            case "whitespace":
                return WhitespaceTokenizer.Instance;
        }

        throw new ArgumentException("An invalid tokenizer type was specified.", nameof(type));
    }

    public static IDetokenizer<string, string> CreateWordDetokenizer(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "latin":
                return new LatinWordDetokenizer();

            case "zwsp":
                return new ZwspWordDetokenizer();

            case "whitespace":
                return WhitespaceDetokenizer.Instance;
        }

        throw new ArgumentException("An invalid tokenizer type was specified.", nameof(type));
    }

    public static ISet<string> GetTexts(IEnumerable<string> values)
    {
        var ids = new HashSet<string>();
        foreach (string value in values)
        {
            foreach (string id in value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (id == "*NT*")
                    ids.UnionWith(Canon.AllBookIds.Where(Canon.IsBookNT));
                else if (id == "*OT*")
                    ids.UnionWith(Canon.AllBookIds.Where(Canon.IsBookOT));
                else
                    ids.Add(id);
            }
        }
        return ids;
    }

    public static bool IsDirectoryPath(string path)
    {
        if (Directory.Exists(path))
            return true;
        string separator1 = Path.DirectorySeparatorChar.ToString();
        string separator2 = Path.AltDirectorySeparatorChar.ToString();
        path = path.TrimEnd();
        return path.EndsWith(separator1) || path.EndsWith(separator2);
    }

    public static string GetTranslationModelConfigFileName(string path)
    {
        if (File.Exists(path))
            return path;
        else if (Directory.Exists(path) || IsDirectoryPath(path))
            return Path.Combine(path, "smt.cfg");
        else
            return path;
    }

    public static bool ValidateTranslationModelTypeOption(string value)
    {
        var validTypes = new HashSet<string>
        {
            ThotWordAlignmentHelpers.Hmm,
            ThotWordAlignmentHelpers.Ibm1,
            ThotWordAlignmentHelpers.Ibm2,
            ThotWordAlignmentHelpers.FastAlign
        };
        return string.IsNullOrEmpty(value) || validTypes.Contains(value);
    }

    public static ITrainer CreateTranslationModelTrainer(
        string modelType,
        string modelConfigFileName,
        IParallelTextCorpus corpus,
        int maxSize
    )
    {
        ThotWordAlignmentModelType wordAlignmentModelType = ThotWordAlignmentHelpers.GetThotWordAlignmentModelType(
            modelType
        );

        string modelDir = Path.GetDirectoryName(modelConfigFileName);
        if (!Directory.Exists(modelDir))
            Directory.CreateDirectory(modelDir);

        if (!File.Exists(modelConfigFileName))
            CreateConfigFile(wordAlignmentModelType, modelConfigFileName);

        return new ThotSmtModelTrainer(wordAlignmentModelType, corpus, modelConfigFileName)
        {
            MaxCorpusCount = maxSize
        };
    }

    public static StreamWriter CreateStreamWriter(string fileName)
    {
        var utf8Encoding = new UTF8Encoding(false);
        return new StreamWriter(fileName, false, utf8Encoding) { NewLine = "\n" };
    }

    private static void CreateConfigFile(ThotWordAlignmentModelType wordAlignmentModelType, string modelConfigFileName)
    {
        string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "default-smt.cfg");
        string text = File.ReadAllText(defaultConfigFileName);
        int emIters = 5;
        if (wordAlignmentModelType == ThotWordAlignmentModelType.FastAlign)
            emIters = 4;
        text = text.Replace("{em_iters}", $"{emIters}");
        File.WriteAllText(modelConfigFileName, text);
    }
}
