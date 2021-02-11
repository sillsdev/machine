using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Statistics;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class LexiconCommand : CommandBase
	{
		private const string Symmetric = "symmetric";
		private const string Direct = "direct";
		private const string Inverse = "inverse";

		private readonly AlignmentModelCommandSpec _modelSpec;
		private readonly CommandArgument _outputArgument;
		private readonly CommandOption _directionOption;
		private readonly CommandOption _probOption;
		private readonly CommandOption _nullOption;
		private readonly CommandOption _thresholdOption;
		private readonly CommandOption _beamThresholdOption;
		private readonly CommandOption _quietOption;

		public LexiconCommand()
		{
			Name = "lexicon";
			Description = "Extracts a lexicon from a word alignment model.";

			_modelSpec = AddSpec(new AlignmentModelCommandSpec { IncludeSymHeuristicOption = false });
			_outputArgument = Argument("OUTPUT_PATH", "The output lexicon file.")
				.IsRequired();
			_directionOption = Option("-d|--direction <DIRECTION>",
				$"The word alignment model direction.\nDirections: \"{Symmetric}\" (default), \"{Direct}\", \"{Inverse}\".",
				CommandOptionType.SingleValue);
			_probOption = Option("-p|--probabilities", "Include probabilities in the output.",
				CommandOptionType.NoValue);
			_nullOption = Option("-n|--null", "Include NULL in the output.",
				CommandOptionType.NoValue);
			_thresholdOption = Option("-t|--threshold <PERCENTAGE>", "The probability threshold.\nThis threshold will override the beam threshold if both are specified.",
				CommandOptionType.SingleValue);
			_beamThresholdOption = Option("-bt|--beam-threshold <PERCENTAGE>", "The beam threshold. Default: 0.02.",
				CommandOptionType.SingleValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!ValidateDirectionOption(_directionOption.Value()))
			{
				Out.WriteLine("The specified direction is invalid.");
				return 1;
			}

			WordAlignmentDirection direction = (_directionOption.Value()) switch
			{
				Direct => WordAlignmentDirection.Direct,
				Inverse => WordAlignmentDirection.Inverse,
				_ => WordAlignmentDirection.Symmetric,
			};

			double threshold = 0.0;
			double beamThreshold = 0.02;
			if (_thresholdOption.HasValue())
			{
				if (!double.TryParse(_thresholdOption.Value(), out threshold))
				{
					Out.WriteLine("The specified probability threshold is invalid.");
					return 1;
				}
				beamThreshold = 0;
			}
			else if (_beamThresholdOption.HasValue())
			{
				if (!double.TryParse(_beamThresholdOption.Value(), out beamThreshold))
				{
					Out.WriteLine("The specified beam threshold is invalid.");
					return 1;
				}
			}
			double logBeamThreshold = beamThreshold == 0 ? LogSpace.Zero : LogSpace.ToLogSpace(beamThreshold);

			if (!_quietOption.HasValue())
				Out.Write("Loading... ");

			using IWordAlignmentModel alignmentModel = _modelSpec.CreateAlignmentModel(direction);
			if (!_quietOption.HasValue())
			{
				Out.WriteLine("done.");
				Out.Write("Extracting lexicon... ");
			}

			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (var writer = new StreamWriter(_outputArgument.Value))
			{
				string[] sourceWords = alignmentModel.SourceWords.ToArray();
				string[] targetWords = alignmentModel.TargetWords.ToArray();
				int stepCount = sourceWords.Length;
				if (!_nullOption.HasValue() && alignmentModel.NullIndex >= 0)
					stepCount--;
				for (int i = 0; i < sourceWords.Length; i++)
				{
					ProcessSourceWord(alignmentModel, sourceWords, targetWords, i, threshold, logBeamThreshold, writer);
					progress?.Report(new ProgressStatus(i + 1, stepCount));
				}
			}

			if (!_quietOption.HasValue())
				Out.WriteLine("done.");

			return 0;
		}

		private void ProcessSourceWord(IWordAlignmentModel alignmentModel, string[] sourceWords,
			string[] targetWords, int i, double threshold, double logBeamThreshold, StreamWriter writer)
		{
			if (!IsWordIncluded(alignmentModel, i))
				return;

			string sourceWord = GetWord(alignmentModel, sourceWords, i);
			var targetWordProbs = new List<(string Word, double Prob)>();
			double maxProb = 0.0;
			double probSum = 0.0;
			for (int j = 0; j < targetWords.Length; j++)
			{
				double prob = alignmentModel.GetTranslationProbability(i, j);
				probSum += prob;
				maxProb = Math.Max(prob, maxProb);
				if (IsWordIncluded(alignmentModel, j) && Math.Round(prob, 15, MidpointRounding.AwayFromZero) > 0)
				{
					string targetWord = GetWord(alignmentModel, targetWords, j);
					targetWordProbs.Add((targetWord, prob));
				}
			}

			if (!alignmentModel.IsProbabilityDistributionNormalized)
				maxProb /= probSum;
			double logSourceWordThreshold = -LogSpace.ToLogSpace(maxProb) * logBeamThreshold;
			IEnumerable<(string Word, double Prob)> query = alignmentModel.IsProbabilityDistributionNormalized
				? targetWordProbs : targetWordProbs.Select(wp => (wp.Word, Prob: wp.Prob / probSum));
			query = query.Where(wp => Math.Round(wp.Prob, 8, MidpointRounding.AwayFromZero) > threshold
					&& LogSpace.ToLogSpace(wp.Prob) >= logSourceWordThreshold)
				.OrderByDescending(wp => wp.Prob);

			foreach ((string targetWord, double prob) in query)
				WriteWordPair(writer, sourceWord, targetWord, prob);
		}

		private void WriteWordPair(StreamWriter writer, string sourceWord, string targetWord, double prob)
		{
			string line = $"{sourceWord}\t{targetWord}";
			if (_probOption.HasValue())
				line += $"\t{prob:0.########}";
			writer.WriteLine(line);
		}

		private bool IsWordIncluded(IWordAlignmentModel alignmentModel, int index)
		{
			return _nullOption.HasValue() || index != alignmentModel.NullIndex;
		}

		private static string GetWord(IWordAlignmentModel alignmentModel, string[] words, int index)
		{
			return index == alignmentModel.NullIndex ? "NULL" : words[index];
		}

		private static bool ValidateDirectionOption(string value)
		{
			var validDirections = new HashSet<string>
			{
				Symmetric,
				Direct,
				Inverse
			};
			return string.IsNullOrEmpty(value) || validDirections.Contains(value);
		}
	}
}
