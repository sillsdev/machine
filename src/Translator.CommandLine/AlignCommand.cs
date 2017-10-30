using McMaster.Extensions.CommandLineUtils;
using SIL.CommandLine;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class AlignCommand : EngineCommandBase
	{
		private readonly CommandOption _outputOption;
		private readonly CommandOption _probOption;

		public AlignCommand()
			: base(true)
		{
			_outputOption = Option("-o|--output <path>", "The output alignment directory.",
				CommandOptionType.SingleValue);
			_probOption = Option("-p|--probabilities", "Include probabilities in the output.",
				CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!_outputOption.HasValue())
			{
				Out.WriteLine("The output alignment directory was not specified");
				return 1;
			}

			if (!Directory.Exists(_outputOption.Value()))
				Directory.CreateDirectory(_outputOption.Value());

			int parallelCorpusCount = GetParallelCorpusCount();

			string tmPrefix = Path.Combine(EngineDirectory, "tm", "src_trg");
			Out.Write("Aligning... ");
			using (var progress = new ConsoleProgressBar(Out))
			using (var swaModel = new ThotSingleWordAlignmentModel(tmPrefix + "_invswm"))
			using (var invSwaModel = new ThotSingleWordAlignmentModel(tmPrefix + "_swm"))
			{
				var aligner = new SymmetrizationSegmentAligner(swaModel, invSwaModel);
				int segmentCount = 0;
				foreach (ParallelText text in ParallelCorpus.Texts)
				{
					string fileName = Path.Combine(_outputOption.Value(), text.Id + ".txt");
					using (var writer = new StreamWriter(fileName))
					{
						foreach (ParallelTextSegment segment in text.Segments)
						{
							if (segment.IsEmpty)
							{
								writer.WriteLine();
							}
							else
							{
								string[] sourceTokens = segment.SourceSegment.Select(Preprocessors.Lowercase)
									.ToArray();
								string[] targetTokens = segment.TargetSegment.Select(Preprocessors.Lowercase)
									.ToArray();
								WordAlignmentMatrix matrix = aligner.GetBestAlignment(sourceTokens, targetTokens,
									segment.CreateAlignmentMatrix(true));
								writer.WriteLine(OutputAlignmentString(swaModel, invSwaModel, _probOption.HasValue(),
									sourceTokens, targetTokens, matrix));
								segmentCount++;
								progress.Report((double) segmentCount / parallelCorpusCount);
								if (segmentCount == MaxParallelCorpusCount)
									break;
							}
						}
					}
					if (segmentCount == MaxParallelCorpusCount)
						break;
				}
			}
			Out.WriteLine("done.");

			return 0;
		}

		private static string OutputAlignmentString(ThotSingleWordAlignmentModel swaModel,
			ThotSingleWordAlignmentModel invSwaModel, bool includeProbs, IReadOnlyList<string> source,
			IReadOnlyList<string> target, WordAlignmentMatrix matrix)
		{
			var sourceIndices = new int[matrix.ColumnCount];
			int[] targetIndices = Enumerable.Repeat(-2, matrix.RowCount).ToArray();
			var alignedIndices = new List<(int SourceIndex, int TargetIndex)>();
			int prev = -1;
			for (int j = 0; j < matrix.ColumnCount; j++)
			{
				bool found = false;
				for (int i = 0; i < matrix.RowCount; i++)
				{
					if (matrix[i, j] == AlignmentType.Aligned)
					{
						if (!found)
							sourceIndices[j] = i;
						if (targetIndices[i] == -2)
							targetIndices[i] = j;
						alignedIndices.Add((i, j));
						prev = i;
						found = true;
					}
				}

				// unaligned indices
				if (!found)
					sourceIndices[j] = prev == -1 ? -1 : matrix.RowCount + prev;
			}

			// all remaining target indices are unaligned, so fill them in
			prev = -1;
			for (int i = 0; i < matrix.RowCount; i++)
			{
				if (targetIndices[i] == -2)
					targetIndices[i] = prev == -1 ? -1 : matrix.ColumnCount + prev;
				else
					prev = targetIndices[i];
			}

			return string.Join(" ", alignedIndices.Select(t => AlignedWordsString(swaModel, invSwaModel, includeProbs, source,
				target, sourceIndices, targetIndices, t.SourceIndex, t.TargetIndex)));
		}

		private static string AlignedWordsString(ThotSingleWordAlignmentModel swaModel, ThotSingleWordAlignmentModel invSwaModel,
			bool includeProbs, IReadOnlyList<string> source, IReadOnlyList<string> target, int[] sourceIndices,
			int[] targetIndices, int sourceIndex, int targetIndex)
		{
			if (includeProbs)
			{
				string sourceWord = source[sourceIndex];
				string targetWord = target[targetIndex];
				double transProb = swaModel.GetTranslationProbability(sourceWord, targetWord);
				double invTransProb = invSwaModel.GetTranslationProbability(targetWord, sourceWord);
				double maxTransProb = Math.Max(transProb, invTransProb);

				double alignProb = swaModel.GetAlignmentProbability(source.Count,
					targetIndex == 0 ? -1 : sourceIndices[targetIndex - 1], sourceIndex);
				double invAlignProb = invSwaModel.GetAlignmentProbability(target.Count,
					sourceIndex == 0 ? -1 : targetIndices[sourceIndex - 1], targetIndex);
				double maxAlignProb = Math.Max(alignProb, invAlignProb);
				return $"{sourceIndex}-{targetIndex}:{transProb:0.########}:{alignProb:0.########}";
			}

			return $"{sourceIndex}-{targetIndex}";
		}
	}
}
