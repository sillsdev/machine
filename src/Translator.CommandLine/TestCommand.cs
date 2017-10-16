using McMaster.Extensions.CommandLineUtils;
using SIL.CommandLine;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class TestCommand : ParallelTextCommand
	{
		private readonly CommandOption _confidenceOption;
		private readonly CommandOption _traceOption;

		private int _actionCount;
		private int _charCount;
		private int _correctSuggestionCount;
		private int _totalSuggestionCount;

		public TestCommand()
			: base(false)
		{
			Name = "test";

			_confidenceOption = Option("-c|--confidence <percentage>", "The confidence threshold.",
				CommandOptionType.SingleValue);
			_traceOption = Option("--trace <path>", "The trace output directory.",
				CommandOptionType.SingleValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!File.Exists(EngineConfigFileName))
			{
				Out.WriteLine("The specified engine directory is invalid.");
				return 1;
			}

			double confidence = 0.2;
			if (_confidenceOption.HasValue())
			{
				if (!double.TryParse(_confidenceOption.Value(), out confidence))
				{
					Out.WriteLine("The specified confidence is invalid.");
					return 1;
				}
			}

			if (_traceOption.HasValue())
			{
				if (!Directory.Exists(_traceOption.Value()))
					Directory.CreateDirectory(_traceOption.Value());
			}

			var suggester = new WordTranslationSuggester(confidence);

			var corpus = new ParallelTextCorpus(SourceCorpus, TargetCorpus);
			int totalSegmentCount = corpus.Texts.SelectMany(t => t.Segments).Count(s => !s.IsEmpty);

			Out.Write("Testing... ");
			int segmentCount = 0;
			using (var progress = new ConsoleProgressBar(Out))
			using (IInteractiveSmtModel smtModel = new ThotSmtModel(EngineConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			{
				foreach (ParallelText text in corpus.Texts)
				{
					using (StreamWriter traceWriter = CreateTraceWriter(text))
					{
						foreach (ParallelTextSegment segment in text.Segments.Where(s => !s.IsEmpty))
						{
							TestSegment(engine, suggester, segment, traceWriter);
							segmentCount++;
							progress.Report((double) segmentCount / totalSegmentCount);
						}
					}
				}
			}
			Out.WriteLine("done.");

			Out.WriteLine($"# of Segments: {segmentCount}");
			Out.WriteLine($"# of Suggestions: {_totalSuggestionCount}");
			Out.WriteLine($"# of Correct Suggestions: {_correctSuggestionCount}");
			double ksmr = (double) _actionCount / _charCount;
			Out.WriteLine($"KSMR: {ksmr:0.00}");
			double precision = (double) _correctSuggestionCount / _totalSuggestionCount;
			Out.WriteLine($"Precision: {precision:0.00}");
			return 0;
		}

		private StreamWriter CreateTraceWriter(ParallelText text)
		{
			if (_traceOption.HasValue())
			{
				string fileName = Path.Combine(_traceOption.Value(), text.Id + "-trace.txt");
				return new StreamWriter(fileName);
			}

			return null;
		}

		private void TestSegment(IInteractiveSmtEngine engine, ITranslationSuggester suggester,
			ParallelTextSegment segment, StreamWriter traceWriter)
		{
			traceWriter?.WriteLine($"Segment:      {segment.SegmentRef}");
			string[] sourceSegment = segment.SourceSegment.Select(Preprocessors.Lowercase).ToArray();
			traceWriter?.WriteLine($"Source:       {string.Join(" ", sourceSegment)}");
			string[] targetSegment = segment.TargetSegment.Select(Preprocessors.Lowercase).ToArray();
			traceWriter?.WriteLine($"Target:       {string.Join(" ", targetSegment)}");
			traceWriter?.WriteLine(new string('=', 120));
			string[] prevSuggestionWords = null;
			bool isLastWordSuggestion = false;
			string suggestionResult = null;
			using (IInteractiveTranslationSession session = engine.TranslateInteractively(sourceSegment))
			{
				while (session.Prefix.Count < targetSegment.Length || !session.IsLastWordComplete)
				{
					int[] suggestion = suggester.GetSuggestedWordIndices(session).ToArray();

					string[] suggestionWords = suggestion.Select(j => session.CurrentResult.TargetSegment[j]).ToArray();
					if (prevSuggestionWords == null || !prevSuggestionWords.SequenceEqual(suggestionWords))
					{
						WritePrefix(traceWriter, suggestionResult, session.Prefix);
						WriteSuggestion(traceWriter, session, suggestion);
						suggestionResult = null;
						if (suggestion.Length > 0)
							_totalSuggestionCount++;
					}

					int targetIndex = session.Prefix.Count;
					if (!session.IsLastWordComplete)
						targetIndex--;

					var accepted = new List<int>();
					for (int i = 0, j = targetIndex; i < suggestionWords.Length && j < targetSegment.Length; i++)
					{
						if (suggestionWords[i] == targetSegment[j])
						{
							accepted.Add(suggestion[i]);
							j++;
						}
						else if (accepted.Count == 0)
						{
							j = targetIndex;
						}
						else
						{
							break;
						}
					}

					if (accepted.Count > 0)
					{
						session.AppendSuggestionToPrefix(accepted);
						isLastWordSuggestion = true;
						_actionCount++;
						_correctSuggestionCount++;
						if (accepted.Count == suggestion.Length)
							suggestionResult = "ACCEPT_FULL";
						else if (accepted[0] == suggestion[0])
							suggestionResult = "ACCEPT_INIT";
						else if (accepted[accepted.Count - 1] == suggestion[suggestion.Length - 1])
							suggestionResult = "ACCEPT_FIN";
						else
							suggestionResult = "ACCEPT_MID";
					}
					else
					{
						if (isLastWordSuggestion)
						{
							_actionCount++;
							isLastWordSuggestion = false;
							WritePrefix(traceWriter, suggestionResult, session.Prefix);
							suggestionResult = null;
						}

						int len = session.IsLastWordComplete ? 0 : session.Prefix[session.Prefix.Count - 1].Length;
						string targetWord = targetSegment[targetIndex];
						if (len == targetWord.Length)
						{
							session.AppendToPrefix("", true);
						}
						else
						{
							string c = targetWord.Substring(len, 1);
							session.AppendToPrefix(c, false);
						}

						suggestionResult = suggestion.Length == 0 ? "NONE" : "REJECT";
						_actionCount++;
					}

					prevSuggestionWords = suggestionWords;
				}

				WritePrefix(traceWriter, suggestionResult, session.Prefix);

				session.Approve();
			}

			_charCount += targetSegment.Sum(w => w.Length + 1);
			traceWriter?.WriteLine();
		}

		private void WritePrefix(StreamWriter traceWriter, string suggestionResult, IReadOnlyList<string> prefix)
		{
			if (traceWriter == null || suggestionResult == null)
				return;

			traceWriter.Write(("-" + suggestionResult).PadRight(14));
			traceWriter.WriteLine(string.Join(" ", prefix));
		}

		private void WriteSuggestion(StreamWriter traceWriter, IInteractiveTranslationSession session,
			int[] suggestion)
		{
			if (traceWriter == null)
				return;

			bool inSuggestion = false;
			traceWriter.Write("SUGGESTION    ");
			for (int j = 0; j < session.CurrentResult.TargetSegment.Count; j++)
			{
				if (suggestion.Contains(j))
				{
					if (j > 0)
						traceWriter.Write(" ");
					if (!inSuggestion)
						traceWriter.Write("[");
					inSuggestion = true;
				}
				else if (inSuggestion)
				{
					traceWriter.Write("] ");
					inSuggestion = false;
				}
				else if (j > 0)
				{
					traceWriter.Write(" ");
				}

				traceWriter.Write(session.CurrentResult.TargetSegment[j]);
			}
			traceWriter.WriteLine();
		}
	}
}
