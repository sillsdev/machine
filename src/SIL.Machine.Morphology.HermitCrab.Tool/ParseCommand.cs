using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ManyConsole;

namespace SIL.Machine.Morphology.HermitCrab;

internal class ParseCommand : ConsoleCommand
{
    private readonly HCContext _context;

    public ParseCommand(HCContext context)
    {
        _context = context;

        IsCommand("parse", "Parses a word");
        SkipsCommandSummaryBeforeRunning();
        HasAdditionalArguments(1, "<word>");
    }

    public override int Run(string[] remainingArguments)
    {
        string word = remainingArguments[0];
        try
        {
            _context.ParseCount++;
            _context.Out.WriteLine("Parsing \"{0}\"", word);
            object trace;
            var watch = Stopwatch.StartNew();
            Word[] results = _context.Morpher.ParseWord(word, out trace).ToArray();
            watch.Stop();
            if (results.Length == 0)
            {
                _context.FailedParseCount++;
                _context.Out.WriteLine("No valid parses.");
            }
            else
            {
                _context.SuccessfulParseCount++;
                for (int i = 0; i < results.Length; i++)
                {
                    _context.Out.WriteLine("Parse {0}", i + 1);
                    _context.Out.WriteParse(results[i].GetMorphInfos());
                }
            }
            if (_context.Morpher.TraceManager.IsTracing)
                PrintTrace((Trace)trace, 0, []);
            _context.Out.WriteLine("Parse time: {0}ms", watch.ElapsedMilliseconds);
            _context.Out.WriteLine();
            return 0;
        }
        catch (InvalidShapeException ise)
        {
            _context.ErrorParseCount++;
            _context.Out.WriteLine("The word contains an invalid segment at position {0}.", ise.Position + 1);
            _context.Out.WriteLine();
            return 1;
        }
    }

    private void PrintTrace(Trace trace, int indent, HashSet<int> lineIndices)
    {
        _context.Out.Write(GetTraceTypeString(trace.Type));
        _context.Out.Write(" [");
        bool first = true;
        string ruleLabel = GetRuleLabelString(trace.Source);
        if (!string.IsNullOrEmpty(ruleLabel))
        {
            if (trace.SubruleIndex >= 0)
                _context.Out.Write("{0}: {1}({2})", ruleLabel, trace.Source.Name, trace.SubruleIndex);
            else
                _context.Out.Write("{0}: {1}", ruleLabel, trace.Source.Name);
            first = false;
        }
        bool analysis = IsAnalysis(trace.Type);
        if (trace.Input != null)
        {
            if (!first)
                _context.Out.Write(", ");
            first = false;
            CharacterDefinitionTable table = trace.Input.Stratum.CharacterDefinitionTable;
            _context.Out.Write(
                "Input: {0}",
                analysis ? trace.Input.Shape.ToRegexString(table, true) : trace.Input.Shape.ToString(table, true)
            );
        }
        if (trace.Output != null)
        {
            if (!first)
                _context.Out.Write(", ");
            first = false;
            CharacterDefinitionTable table = trace.Output.Stratum.CharacterDefinitionTable;
            _context.Out.Write(
                "Output: {0}",
                analysis ? trace.Output.Shape.ToRegexString(table, true) : trace.Output.Shape.ToString(table, true)
            );
        }
        if (trace.FailureReason != FailureReason.None)
        {
            if (!first)
                _context.Out.Write(", ");
            _context.Out.Write("Reason: {0}", trace.FailureReason);
        }
        _context.Out.WriteLine("]");

        if (!trace.IsLeaf)
        {
            int i = 0;
            foreach (Trace child in trace.Children)
            {
                PrintIndent(indent, lineIndices);
                _context.Out.WriteLine("|");
                PrintIndent(indent, lineIndices);
                _context.Out.Write("+-");
                if (i != trace.Children.Count - 1)
                    lineIndices.Add(indent);
                PrintTrace(child, indent + 2, lineIndices);
                if (i != trace.Children.Count - 1)
                    lineIndices.Remove(indent);
                i++;
            }
        }
    }

    private void PrintIndent(int indent, HashSet<int> lineIndices)
    {
        for (int i = 0; i < indent; i++)
            _context.Out.Write(lineIndices.Contains(i) ? "|" : " ");
    }

    private static bool IsAnalysis(TraceType type)
    {
        switch (type)
        {
            case TraceType.WordAnalysis:
            case TraceType.LexicalLookup:
            case TraceType.StratumAnalysisInput:
            case TraceType.StratumAnalysisOutput:
            case TraceType.TemplateAnalysisInput:
            case TraceType.TemplateAnalysisOutput:
            case TraceType.MorphologicalRuleAnalysis:
            case TraceType.PhonologicalRuleAnalysis:
                return true;

            case TraceType.WordSynthesis:
            case TraceType.Successful:
            case TraceType.Failed:
            case TraceType.Blocked:
            case TraceType.StratumSynthesisInput:
            case TraceType.StratumSynthesisOutput:
            case TraceType.TemplateSynthesisInput:
            case TraceType.TemplateSynthesisOutput:
            case TraceType.MorphologicalRuleSynthesis:
            case TraceType.PhonologicalRuleSynthesis:
                return false;
        }

        return false;
    }

    private static string GetRuleLabelString(IHCRule rule)
    {
        if (rule is Stratum)
            return "Stratum";
        if (rule is IMorphologicalRule || rule is IPhonologicalRule)
            return "Rule";
        if (rule is AffixTemplate)
            return "Template";
        return null;
    }

    private static string GetTraceTypeString(TraceType type)
    {
        switch (type)
        {
            case TraceType.WordAnalysis:
                return "Word Analysis";
            case TraceType.WordSynthesis:
                return "Word Synthesis";
            case TraceType.Successful:
                return "Successful Parse";
            case TraceType.Failed:
                return "Failed Parse";
            case TraceType.Blocked:
                return "Blocked Parse";
            case TraceType.LexicalLookup:
                return "Lexical Lookup";
            case TraceType.StratumAnalysisInput:
                return "Stratum Analysis In";
            case TraceType.StratumAnalysisOutput:
                return "Stratum Analysis Out";
            case TraceType.StratumSynthesisInput:
                return "Stratum Synthesis In";
            case TraceType.StratumSynthesisOutput:
                return "Stratum Synthesis Out";
            case TraceType.TemplateAnalysisInput:
                return "Template Analysis In";
            case TraceType.TemplateAnalysisOutput:
                return "Template Analysis Out";
            case TraceType.TemplateSynthesisInput:
                return "Template Synthesis In";
            case TraceType.TemplateSynthesisOutput:
                return "Template Synthesis Out";
            case TraceType.MorphologicalRuleAnalysis:
                return "Morphological Rule Analysis";
            case TraceType.MorphologicalRuleSynthesis:
                return "Morphological Rule Synthesis";
            case TraceType.PhonologicalRuleAnalysis:
                return "Phonological Rule Analysis";
            case TraceType.PhonologicalRuleSynthesis:
                return "Phonological Rule Synthesis";
        }

        return null;
    }
}
