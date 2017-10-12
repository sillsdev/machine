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

					bool match;
					if (suggestion.Length == 0)
					{
						match = false;
					}
					else
					{
						match = true;
						int j = targetIndex;
						foreach (string word in suggestionWords)
						{
							if (j >= targetSegment.Length || word != targetSegment[j])
							{
								match = false;
								break;
							}
						}
					}

					if (match)
					{
						// full suggestion match
						session.AppendSuggestionToPrefix(suggestion);
						isLastWordSuggestion = true;
						_actionCount++;
						_correctSuggestionCount++;
						suggestionResult = "ACCEPT_FULL";
					}
					else
					{
						int suggestionIndex = 0;
						foreach (string word in suggestionWords)
						{
							if (word == targetSegment[targetIndex])
							{
								match = true;
								break;
							}
							suggestionIndex++;
						}

						if (match)
						{
							// single word suggestion match
							session.AppendSuggestionToPrefix(suggestion, suggestionIndex);
							isLastWordSuggestion = true;
							_actionCount++;
							_correctSuggestionCount++;
							suggestionResult = suggestionIndex == 0 ? "ACCEPT_INIT" : "ACCEPT_MID";
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

			traceWriter.Write("SUGGESTION    ");
			bool firstPhrase = true;
			foreach (Phrase phrase in session.CurrentResult.Phrases)
			{
				if (!firstPhrase)
				{
					if (phrase.TargetSegmentRange.Start >= session.Prefix.Count)
						traceWriter.Write(" | ");
					else traceWriter.Write(" ");
				}

				for (int j = phrase.TargetSegmentRange.Start; j < phrase.TargetSegmentRange.End; j++)
				{
					if (j != phrase.TargetSegmentRange.Start)
						traceWriter.Write(" ");
					if (suggestion.Contains(j))
						traceWriter.Write("+");
					traceWriter.Write(session.CurrentResult.TargetSegment[j]);
				}

				firstPhrase = false;
			}
			traceWriter.WriteLine();
		}
	}
}
