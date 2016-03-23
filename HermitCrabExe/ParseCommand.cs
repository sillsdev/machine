using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ManyConsole;
using SIL.Machine.Annotations;

namespace SIL.Machine.HermitCrab
{
	public class ParseCommand : ConsoleCommand
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
			object trace;
			Stopwatch watch = Stopwatch.StartNew();
			Word[] results = _context.Morpher.ParseWord(word, out trace).ToArray();
			watch.Stop();
			_context.Out.WriteLine("Parsing \"{0}\"", word);
			if (results.Length == 0)
			{
				_context.Out.WriteLine("No valid parses.");
				_context.Out.WriteLine();
			}
			else
			{
				foreach (Word result in results)
					PrintResult(result);
			}
			if (_context.Morpher.TraceManager.IsTracing)
			{
				PrintTrace((Trace) trace, 0, new HashSet<int>());
				_context.Out.WriteLine();
			}
			_context.Out.WriteLine("Parse time: {0}ms", watch.ElapsedMilliseconds);
			return 0;
		}

		private void PrintResult(Word result)
		{
			_context.Out.Write("Morphs: ");
			bool firstItem = true;
			foreach (Annotation<ShapeNode> morph in result.Morphs)
			{
				Allomorph allomorph = result.GetAllomorph(morph);
				string gloss = string.IsNullOrEmpty(allomorph.Morpheme.Gloss) ? "?" : allomorph.Morpheme.Gloss;
				string morphStr = result.Shape.GetNodes(morph.Span).ToString(result.Stratum.SymbolTable, false);
				int len = Math.Max(morphStr.Length, gloss.Length);
				if (len > 0)
				{
					if (!firstItem)
						_context.Out.Write(" ");
					_context.Out.Write(morphStr.PadRight(len));
					firstItem = false;
				}
			}
			_context.Out.WriteLine();
			_context.Out.Write("Gloss:  ");
			firstItem = true;
			foreach (Annotation<ShapeNode> morph in result.Morphs)
			{
				Allomorph allomorph = result.GetAllomorph(morph);
				string gloss = string.IsNullOrEmpty(allomorph.Morpheme.Gloss) ? "?" : allomorph.Morpheme.Gloss;
				string morphStr = result.Shape.GetNodes(morph.Span).ToString(result.Stratum.SymbolTable, false);
				int len = Math.Max(morphStr.Length, gloss.Length);
				if (len > 0)
				{
					if (!firstItem)
						_context.Out.Write(" ");
					_context.Out.Write(gloss.PadRight(len));
					firstItem = false;
				}
			}
			_context.Out.WriteLine();
			_context.Out.WriteLine();
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
				SymbolTable table = trace.Input.Stratum.SymbolTable;
				_context.Out.Write("Input: {0}", analysis ? trace.Input.Shape.ToRegexString(table, true) : trace.Input.Shape.ToString(table, true));
			}
			if (trace.Output != null)
			{
				if (!first)
					_context.Out.Write(", ");
				first = false;
				SymbolTable table = trace.Output.Stratum.SymbolTable;
				_context.Out.Write("Output: {0}", analysis ? trace.Output.Shape.ToRegexString(table, true) : trace.Output.Shape.ToString(table, true));
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
}
