using McMaster.Extensions.CommandLineUtils;
using SIL.CommandLine;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;
using System.IO;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class TestCommand : ParallelTextCommand
	{
		private readonly CommandOption _confidenceOption;

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
					foreach (ParallelTextSegment segment in text.Segments.Where(s => !s.IsEmpty))
					{
						TestSegment(engine, confidence, segment);
						segmentCount++;
						progress.Report((double) segmentCount / totalSegmentCount);
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

		private void TestSegment(IInteractiveSmtEngine engine, double confidence, ParallelTextSegment segment)
		{
			string[] sourceSegment = segment.SourceSegment.Select(Preprocessors.Lowercase).ToArray();
			string[] targetSegment = segment.TargetSegment.Select(Preprocessors.Lowercase).ToArray();
			int[] prevSuggestion = null;
			bool isLastWordSuggestion = false;
			using (IInteractiveTranslationSession session = engine.TranslateInteractively(sourceSegment))
			{
				while (session.Prefix.Count < targetSegment.Length || !session.IsLastWordComplete)
				{
					int[] suggestion = session.GetSuggestedWordIndices(confidence).ToArray();

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
						if (prevSuggestion == null || !prevSuggestion.SequenceEqual(suggestion))
							_totalSuggestionCount++;

						match = true;
						for (int i = 0; i < suggestion.Length; i++)
						{
							int suggestionIndex = suggestion[i];
							int j = targetIndex + i;
							if (j >= targetSegment.Length
								|| session.CurrentResult.TargetSegment[suggestionIndex] != targetSegment[j])
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
					}
					else
					{
						int suggestionIndex = 0;
						foreach (int j in suggestion)
						{
							if (session.CurrentResult.TargetSegment[j] == targetSegment[targetIndex])
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
						}
						else
						{
							if (isLastWordSuggestion)
							{
								_actionCount++;
								isLastWordSuggestion = false;
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

							_actionCount++;
						}
					}

					prevSuggestion = suggestion;
				}

				session.Approve();
			}

			_charCount += targetSegment.Sum(w => w.Length + 1);
		}
	}
}
