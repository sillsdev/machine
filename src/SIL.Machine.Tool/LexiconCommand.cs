using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class LexiconCommand : CommandBase
	{
		private readonly AlignmentModelCommandSpec _modelSpec;
		private readonly CommandArgument _outputArgument;
		private readonly CommandOption _probOption;
		private readonly CommandOption _thresholdOption;
		private readonly CommandOption _quietOption;

		public LexiconCommand()
		{
			Name = "lexicon";
			Description = "Extracts a lexicon from a word alignment model.";

			_modelSpec = AddSpec(new AlignmentModelCommandSpec());
			_outputArgument = Argument("OUTPUT_PATH", "The output lexicon file.")
				.IsRequired();
			_probOption = Option("-p|--probabilities", "Include probabilities in the output.",
				CommandOptionType.NoValue);
			_thresholdOption = Option("-t|--threshold <PROBABILITY>", "The probability threshold. Default: 0.01.",
				CommandOptionType.SingleValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			double threshold = 0.01;
			if (_thresholdOption.HasValue())
			{
				if (!double.TryParse(_thresholdOption.Value(), out threshold))
				{
					Out.WriteLine("The specified probability threshold is invalid.");
					return 1;
				}
			}

			if (!_quietOption.HasValue())
				Out.Write("Loading... ");

			using IWordAlignmentModel alignmentModel = _modelSpec.CreateAlignmentModel();
			if (!_quietOption.HasValue())
			{
				Out.WriteLine("done.");
				Out.Write("Extracting lexicon... ");
			}

			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (var writer = new StreamWriter(_outputArgument.Value))
			{
				int sourceWordCount = alignmentModel.SourceWords.Count;
				int targetWordCount = alignmentModel.TargetWords.Count;
				int stepCount = sourceWordCount;
				if (alignmentModel.NullIndex >= 0)
					stepCount--;
				for (int i = 0; i < sourceWordCount; i++)
				{
					if (i == alignmentModel.NullIndex)
						continue;

					string sourceWord = alignmentModel.SourceWords[i];
					var targetWords = new List<(string Word, double Prob)>();
					for (int j = 0; j < targetWordCount; j++)
					{
						if (j == alignmentModel.NullIndex)
							continue;

						double prob = alignmentModel.GetTranslationProbability(i, j);
						prob = Math.Round(prob, 8, MidpointRounding.AwayFromZero);
						if (prob > threshold)
						{
							string targetWord = alignmentModel.TargetWords[j];
							targetWords.Add((targetWord, prob));
						}
					}

					foreach ((string word, double prob) in targetWords.OrderByDescending(wp => wp.Prob))
					{
						string line = $"{sourceWord}\t{word}";
						if (_probOption.HasValue())
							line += $"\t{prob:0.########}";
						writer.WriteLine(line);
					}

					progress?.Report(new ProgressStatus(i + 1, stepCount));
				}
			}

			if (!_quietOption.HasValue())
				Out.WriteLine("done.");

			return 0;
		}
	}
}
