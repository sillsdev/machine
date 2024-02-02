﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Statistics;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine
{
    public class ExtractLexiconCommand : CommandBase
    {
        private const string Symmetric = "symmetric";
        private const string Direct = "direct";
        private const string Inverse = "inverse";

        private readonly AlignmentModelCommandSpec _modelSpec;
        private readonly CommandArgument _outputArgument;
        private readonly CommandOption _directionOption;
        private readonly CommandOption _probOption;
        private readonly CommandOption _specialSymbolsOption;
        private readonly CommandOption _thresholdOption;
        private readonly CommandOption _beamThresholdOption;
        private readonly CommandOption _quietOption;

        public ExtractLexiconCommand()
        {
            Name = "extract-lexicon";
            Description = "Extracts a lexicon from a word alignment model.";

            _modelSpec = AddSpec(new AlignmentModelCommandSpec());
            _outputArgument = Argument("OUTPUT_PATH", "The output lexicon file.").IsRequired();
            _directionOption = Option(
                "-d|--direction <DIRECTION>",
                $"The word alignment model direction.\nDirections: \"{Symmetric}\" (default), \"{Direct}\", \"{Inverse}\".",
                CommandOptionType.SingleValue
            );
            _probOption = Option(
                "-p|--probabilities",
                "Include probabilities in the output.",
                CommandOptionType.NoValue
            );
            _specialSymbolsOption = Option(
                "-ss|--special-symbols",
                "Include special symbols in the lexicon.",
                CommandOptionType.NoValue
            );
            _thresholdOption = Option(
                "-t|--threshold <PERCENTAGE>",
                "The probability threshold.\nThis threshold will override the beam threshold if both are specified.",
                CommandOptionType.SingleValue
            );
            _beamThresholdOption = Option(
                "-bt|--beam-threshold <PERCENTAGE>",
                "The beam threshold. Default: 0.02.",
                CommandOptionType.SingleValue
            );
            _quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
        }

        protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
        {
            int code = await base.ExecuteCommandAsync(ct);
            if (code != 0)
                return code;

            if (!ValidateDirectionOption(_directionOption.Value()))
            {
                Out.WriteLine("The specified direction is invalid.");
                return 1;
            }

            string directionStr = (_directionOption.Value() ?? Symmetric).ToLowerInvariant();
            WordAlignmentDirection direction = (directionStr) switch
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
                Out.Write("Loading model... ");

            using IWordAlignmentModel alignmentModel = _modelSpec.CreateAlignmentModel(direction);
            if (!_quietOption.HasValue())
            {
                Out.WriteLine("done.");
                Out.Write($"Extracting {directionStr} lexicon... ");
            }

            using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
            using (StreamWriter writer = ToolHelpers.CreateStreamWriter(_outputArgument.Value))
            {
                string[] sourceWords = alignmentModel.SourceWords.ToArray();
                string[] targetWords = alignmentModel.TargetWords.ToArray();
                int stepCount = sourceWords.Length;
                if (!_specialSymbolsOption.HasValue())
                    stepCount -= alignmentModel.SpecialSymbolIndices.Count;
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

        private void ProcessSourceWord(
            IWordAlignmentModel alignmentModel,
            string[] sourceWords,
            string[] targetWords,
            int i,
            double threshold,
            double logBeamThreshold,
            StreamWriter writer
        )
        {
            if (!IsWordIncluded(alignmentModel, i))
                return;

            bool areScoresNormalized = alignmentModel is IIbm1WordAlignmentModel;
            string sourceWord = sourceWords[i];
            var targetWordScores = new List<(string Word, double Score)>();
            double maxScore = 0.0;
            double scoreSum = 0.0;
            foreach ((int j, double score) in alignmentModel.GetTranslations(i))
            {
                scoreSum += score;
                maxScore = Math.Max(score, maxScore);
                if (IsWordIncluded(alignmentModel, j) && Math.Round(score, 15, MidpointRounding.AwayFromZero) > 0)
                {
                    string targetWord = targetWords[j];
                    targetWordScores.Add((targetWord, score));
                }
            }

            if (!areScoresNormalized)
                maxScore /= scoreSum;
            double logSourceWordThreshold = -LogSpace.ToLogSpace(maxScore) * logBeamThreshold;
            IEnumerable<(string Word, double Prob)> query = areScoresNormalized
                ? targetWordScores
                : targetWordScores.Select(wp => (wp.Word, Prob: wp.Score / scoreSum));
            query = query
                .Where(wp =>
                    Math.Round(wp.Prob, 8, MidpointRounding.AwayFromZero) > threshold
                    && LogSpace.ToLogSpace(wp.Prob) >= logSourceWordThreshold
                )
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
            return _specialSymbolsOption.HasValue() || !alignmentModel.SpecialSymbolIndices.Contains(index);
        }

        private static bool ValidateDirectionOption(string value)
        {
            var validDirections = new HashSet<string> { Symmetric, Direct, Inverse };
            return string.IsNullOrEmpty(value) || validDirections.Contains(value.ToLowerInvariant());
        }
    }
}
