using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class TestCommand : TranslationModelCommandBase
	{
		private readonly CommandOption _confidenceOption;
		private readonly CommandOption _traceOption;
		private readonly CommandOption _nOption;
		private readonly CommandOption _quietOption;
		private readonly CommandOption _approveAlignedOption;

		private int _actionCount;
		private int _charCount;
		private int _totalAcceptedSuggestionCount;
		private int _totalSuggestionCount;
		private int _fullSuggestionCount;
		private int _initSuggestionCount;
		private int _finalSuggestionCount;
		private int _middleSuggestionCount;
		private int[] _acceptedSuggestionCounts;

		public TestCommand()
			: base(false)
		{
			Name = "test";
			Description = "Tests the interactive machine translation performance of a model.";

			_confidenceOption = Option("-c|--confidence <percentage>", "The confidence threshold.",
				CommandOptionType.SingleValue);
			_nOption = Option("-n <number>", "The number of suggestions to generate.",
				CommandOptionType.SingleValue);
			_traceOption = Option("--trace <path>", "The trace output directory.",
				CommandOptionType.SingleValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
			_approveAlignedOption = Option("--approve-aligned", "Approve aligned part of source segment.",
				CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!File.Exists(ModelConfigFileName))
			{
				Out.WriteLine("The specified engine directory is invalid.");
				return 1;
			}

			double confidenceThreshold = 0.2;
			if (_confidenceOption.HasValue())
			{
				if (!double.TryParse(_confidenceOption.Value(), out confidenceThreshold))
				{
					Out.WriteLine("The specified confidence is invalid.");
					return 1;
				}
			}

			int n = 1;
			if (_nOption.HasValue())
			{
				if (!int.TryParse(_nOption.Value(), out n))
				{
					Out.WriteLine("The specified number of suggestions is invalid.");
					return 1;
				}
			}

			if (_traceOption.HasValue())
			{
				if (!Directory.Exists(_traceOption.Value()))
					Directory.CreateDirectory(_traceOption.Value());
			}

			var suggester = new PhraseTranslationSuggester() { ConfidenceThreshold = confidenceThreshold };

			int parallelCorpusCount = GetParallelCorpusCount();

			var watch = Stopwatch.StartNew();
			if (!_quietOption.HasValue())
				Out.Write("Testing... ");
			int segmentCount = 0;
			_acceptedSuggestionCounts = new int[n];
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (IInteractiveTranslationModel smtModel = new ThotSmtModel(ModelConfigFileName))
			using (IInteractiveTranslationEngine engine = smtModel.CreateInteractiveEngine())
			{
				var ecm = new ErrorCorrectionModel();
				progress?.Report(new ProgressStatus(segmentCount, parallelCorpusCount));
				foreach (ParallelText text in ParallelCorpus.Texts)
				{
					using (StreamWriter traceWriter = CreateTraceWriter(text))
					{
						foreach (ParallelTextSegment segment in text.Segments.Where(s => !s.IsEmpty))
						{
							TestSegment(ecm, engine, suggester, n, segment, traceWriter);
							segmentCount++;
							progress?.Report(new ProgressStatus(segmentCount, parallelCorpusCount));
							if (segmentCount == MaxParallelCorpusCount)
								break;
						}
					}
					if (segmentCount == MaxParallelCorpusCount)
						break;
				}
			}
			if (!_quietOption.HasValue())
				Out.WriteLine("done.");
			watch.Stop();

			Out.WriteLine($"Execution time: {watch.Elapsed:c}");
			Out.WriteLine($"# of Segments: {segmentCount}");
			Out.WriteLine($"# of Suggestions: {_totalSuggestionCount}");
			Out.WriteLine($"# of Correct Suggestions: {_totalAcceptedSuggestionCount}");
			Out.WriteLine("Correct Suggestion Types");
			double fullPcnt = (double) _fullSuggestionCount / _totalAcceptedSuggestionCount;
			Out.WriteLine($"-Full: {fullPcnt:0.0000}");
			double initPcnt = (double) _initSuggestionCount / _totalAcceptedSuggestionCount;
			Out.WriteLine($"-Initial: {initPcnt:0.0000}");
			double finalPcnt = (double) _finalSuggestionCount / _totalAcceptedSuggestionCount;
			Out.WriteLine($"-Final: {finalPcnt:0.0000}");
			double middlePcnt = (double) _middleSuggestionCount / _totalAcceptedSuggestionCount;
			Out.WriteLine($"-Middle: {middlePcnt:0.0000}");
			Out.WriteLine("Correct Suggestion N");
			for (int i = 0; i < _acceptedSuggestionCounts.Length; i++)
			{
				double pcnt = (double) _acceptedSuggestionCounts[i] / _totalAcceptedSuggestionCount;
				Out.WriteLine($"-{i + 1}: {pcnt:0.0000}");
			}
			double ksmr = (double) _actionCount / _charCount;
			Out.WriteLine($"KSMR: {ksmr:0.0000}");
			double precision = (double) _totalAcceptedSuggestionCount / _totalSuggestionCount;
			Out.WriteLine($"Precision: {precision:0.0000}");
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

		private void TestSegment(ErrorCorrectionModel ecm, IInteractiveTranslationEngine engine,
			ITranslationSuggester suggester, int n, ParallelTextSegment segment, StreamWriter traceWriter)
		{
			traceWriter?.WriteLine($"Segment:      {segment.SegmentRef}");
			IReadOnlyList<string> sourceSegment = TokenProcessors.Lowercase.Process(segment.SourceSegment);
			traceWriter?.WriteLine($"Source:       {string.Join(" ", sourceSegment)}");
			IReadOnlyList<string> targetSegment = TokenProcessors.Lowercase.Process(segment.TargetSegment);
			traceWriter?.WriteLine($"Target:       {string.Join(" ", targetSegment)}");
			traceWriter?.WriteLine(new string('=', 120));
			string[][] prevSuggestionWords = null;
			bool isLastWordSuggestion = false;
			string suggestionResult = null;
			var translator = InteractiveTranslator.Create(ecm, engine, sourceSegment);
			while (translator.Prefix.Count < targetSegment.Count || !translator.IsLastWordComplete)
			{
				int targetIndex = translator.Prefix.Count;
				if (!translator.IsLastWordComplete)
					targetIndex--;

				bool match = false;
				TranslationSuggestion[] suggestions = suggester.GetSuggestions(translator).Take(n).ToArray();
				string[][] suggestionWords = suggestions.Select(s => s.TargetWords.ToArray()).ToArray();
				if (prevSuggestionWords == null || !SuggestionsAreEqual(prevSuggestionWords, suggestionWords))
				{
					WritePrefix(traceWriter, suggestionResult, translator.Prefix);
					WriteSuggestions(traceWriter, translator, suggestions);
					suggestionResult = null;
					if (suggestions.Any(s => s.TargetWordIndices.Count > 0))
						_totalSuggestionCount++;
				}
				for (int k = 0; k < suggestions.Length; k++)
				{
					TranslationSuggestion suggestion = suggestions[k];
					var accepted = new List<int>();
					for (int i = 0, j = targetIndex; i < suggestionWords[k].Length && j < targetSegment.Count; i++)
					{
						if (suggestionWords[k][i] == targetSegment[j])
						{
							accepted.Add(suggestion.TargetWordIndices[i]);
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
						translator.AppendToPrefix(accepted.Select(j => suggestion.Result.TargetSegment[j]));
						isLastWordSuggestion = true;
						_actionCount++;
						_totalAcceptedSuggestionCount++;
						if (accepted.Count == suggestion.TargetWordIndices.Count)
						{
							suggestionResult = "ACCEPT_FULL";
							_fullSuggestionCount++;
						}
						else if (accepted[0] == suggestion.TargetWordIndices[0])
						{
							suggestionResult = "ACCEPT_INIT";
							_initSuggestionCount++;
						}
						else if (accepted[accepted.Count - 1]
							== suggestion.TargetWordIndices[suggestion.TargetWordIndices.Count - 1])
						{
							suggestionResult = "ACCEPT_FIN";
							_finalSuggestionCount++;
						}
						else
						{
							suggestionResult = "ACCEPT_MID";
							_middleSuggestionCount++;
						}
						_acceptedSuggestionCounts[k]++;
						match = true;
						break;
					}
				}

				if (!match)
				{
					if (isLastWordSuggestion)
					{
						_actionCount++;
						isLastWordSuggestion = false;
						WritePrefix(traceWriter, suggestionResult, translator.Prefix);
						suggestionResult = null;
					}

					int len = translator.IsLastWordComplete ? 0 : translator.Prefix[translator.Prefix.Count - 1].Length;
					string targetWord = targetSegment[targetIndex];
					if (len == targetWord.Length)
					{
						translator.AppendToPrefix("", true);
					}
					else
					{
						string c = targetWord.Substring(len, 1);
						translator.AppendToPrefix(c, false);
					}

					suggestionResult = suggestions.Any(s => s.TargetWordIndices.Count > 0) ? "REJECT" : "NONE";
					_actionCount++;
				}

				prevSuggestionWords = suggestionWords;
			}

			WritePrefix(traceWriter, suggestionResult, translator.Prefix);

			translator.Approve(_approveAlignedOption.HasValue());

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

		private void WriteSuggestions(StreamWriter traceWriter, InteractiveTranslator translator,
			IReadOnlyList<TranslationSuggestion> suggestions)
		{
			if (traceWriter == null)
				return;

			for (int k = 0; k < suggestions.Count; k++)
			{
				TranslationSuggestion suggestion = suggestions[k];
				bool inSuggestion = false;
				traceWriter.Write($"SUGGESTION {k + 1}  ");
				for (int j = 0; j < suggestion.Result.TargetSegment.Count; j++)
				{
					if (suggestion.TargetWordIndices.Contains(j))
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

					traceWriter.Write(suggestion.Result.TargetSegment[j]);
				}
				if (inSuggestion)
					traceWriter.Write("]");
				traceWriter.WriteLine();
			}
		}

		private bool SuggestionsAreEqual(string[][] x, string[][] y)
		{
			if (x.Length != y.Length)
				return false;

			for (int i = 0; i < x.Length; i++)
			{
				if (!x[i].SequenceEqual(y[i]))
					return false;
			}

			return true;
		}
	}
}
