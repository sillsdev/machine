using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SIL.Machine.Morphology.HermitCrab.WhyNot;

public static class WhyNotCli
{
    private const string Usage =
        "Usage: hc-whynot why-not <grammar.xml> <queries.jsonl> <output.jsonl>\n"
        + "  Each query is {\"word\": ..., \"expect\": [{\"morpheme\": ..., \"allomorph\": ...}, ...]}; one whynot.v1\n"
        + "  object is written per query. Exit 0 when the batch ran, 2 for bad arguments, 1 for an unreadable grammar.\n"
        + "       hc-whynot <grammar.xml> <word> --expect <morpheme[:allomorph],...>\n"
        + "  Prints one whynot.v1 object for a single word.";

    public static int Run(string[] args)
    {
        if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help"))
        {
            Console.WriteLine(Usage);
            return 0;
        }
        if (args.Length == 4 && args[0] == "why-not")
            return Batch(args[1], args[2], args[3]);
        if (args.Length == 4 && args[2] == "--expect")
            return Single(args[0], args[1], args[3]);
        Console.Error.WriteLine(Usage);
        return 2;
    }

    private static bool TryLoad(string grammar, out Language language, out GrammarIds ids)
    {
        language = null;
        ids = null;
        try
        {
            language = XmlLanguageLoader.Load(grammar);
            ids = GrammarIds.Load(grammar, language);
            return true;
        }
        catch (Exception e)
            when (e is IOException
                || e is System.Xml.XmlException
                || e is InvalidOperationException
                || e is CompileException
            )
        {
            Console.Error.WriteLine($"cannot load grammar: {e.Message}");
            return false;
        }
    }

    private static bool TryCompile(Language language, out WhyNotSession session)
    {
        session = null;
        try
        {
            session = new WhyNotSession(language);
            return true;
        }
        catch (CompileException e)
        {
            Console.Error.WriteLine($"cannot compile grammar: {e.Message}");
            return false;
        }
    }

    private static int Single(string grammar, string word, string expectArg)
    {
        if (!TryLoad(grammar, out Language language, out GrammarIds ids))
            return 1;
        var expect = new List<(string, string)>();
        foreach (string part in expectArg.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pieces = part.Trim().Split(':');
            expect.Add((pieces[0], pieces.Length > 1 ? pieces[1] : null));
        }
        Console.OutputEncoding = new UTF8Encoding(false);
        if (!TryCompile(language, out WhyNotSession session))
            return 1;
        Console.WriteLine(Answer(session, ids, word, expect, indented: true));
        return 0;
    }

    private static int Batch(string grammar, string queriesPath, string outputPath)
    {
        if (!TryLoad(grammar, out Language language, out GrammarIds ids))
            return 1;
        if (!TryCompile(language, out WhyNotSession session))
            return 1;
        using var output = new StreamWriter(outputPath, false, new UTF8Encoding(false));
        foreach (string line in File.ReadLines(queriesPath).Where(l => l.Trim().Length > 0))
            output.WriteLine(AnswerLine(session, ids, line));
        return 0;
    }

    private static string AnswerLine(WhyNotSession session, GrammarIds ids, string line)
    {
        string word = "";
        var expect = new List<(string, string)>();
        try
        {
            using JsonDocument query = JsonDocument.Parse(line);
            word = query.RootElement.GetProperty("word").GetString();
            foreach (JsonElement morph in query.RootElement.GetProperty("expect").EnumerateArray())
            {
                string allomorph = morph.TryGetProperty("allomorph", out JsonElement a) ? a.GetString() : null;
                expect.Add((morph.GetProperty("morpheme").GetString(), allomorph));
            }
            return Answer(session, ids, word, expect, indented: false);
        }
        catch (Exception e) when (e is JsonException || e is KeyNotFoundException || e is InvalidOperationException)
        {
            return Unknown(
                ids,
                word,
                expect.Select(x => new ExpectedMorph(x.Item1, null, x.Item2)).ToArray(),
                "error: " + e.GetType().Name + ": " + e.Message,
                false
            );
        }
    }

    private static string Unknown(
        GrammarIds ids,
        string word,
        IReadOnlyList<ExpectedMorph> expect,
        string reason,
        bool indented
    ) =>
        WhyNotJson.Serialize(
            new WhyNotResult(word, expect, WhyNotOutcome.Unknown, null, Array.Empty<DerivationResult>(), reason),
            ids,
            indented
        );

    private static string Answer(
        WhyNotSession session,
        GrammarIds ids,
        string word,
        List<(string Morpheme, string Allomorph)> expect,
        bool indented
    )
    {
        var echoed = expect.Select(e => new ExpectedMorph(e.Morpheme, null, e.Allomorph)).ToArray();
        var expected = new List<ExpectedMorph>();
        foreach ((string morpheme, string allomorph) in expect)
        {
            int? index = allomorph == null ? null : ids.AllomorphIndex(morpheme, allomorph);
            if (allomorph != null && index == null)
                return Unknown(ids, word, echoed, "unknown_allomorph_id", indented);
            expected.Add(new ExpectedMorph(morpheme, index, allomorph));
        }
        try
        {
            return WhyNotJson.Serialize(session.Analyze(word, expected), ids, indented);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // A word HermitCrab cannot process fails this query only, never the batch.
            return Unknown(ids, word, echoed, "error: " + e.GetType().Name + ": " + e.Message, indented);
        }
    }
}
