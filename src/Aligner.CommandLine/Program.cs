using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using SIL.CommandLine;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var app = new CommandLineApplication(false)
			{
				Name = "aligner",
				FullName = "Machine Aligner"
			};
			app.HelpOption("-?|-h|--help");
			string version = Assembly.GetEntryAssembly().GetName().Version.ToString();                                           
			app.VersionOption("-v|--version", version);

			CommandOption corpus1Option = app.Option("-c1|--corpus1 <path>", "The first corpus file(s).",
				CommandOptionType.MultipleValue);
			CommandOption corpus2Option = app.Option("-c2|--corpus2 <path>", "The second corpus file(s).",
				CommandOptionType.MultipleValue);
			CommandOption alignmentsOption = app.Option("-a|--alignments <path>", "The partial alignment file(s).",
				CommandOptionType.MultipleValue);
			CommandOption outputOption = app.Option("-o|--output <path>", "The output alignment file(s).",
				CommandOptionType.MultipleValue);
			CommandOption probOption = app.Option("-p|--probabilities", "Include probabilities in the output.",
				CommandOptionType.NoValue);


			app.OnExecute(() =>
				{
					if (args.Length == 0)
					{
						app.ShowHelp();
						return 0;
					}

					if (!corpus1Option.HasValue())
					{
						app.Out.WriteLine("A file was not specified for the first corpus.");
						return 1;
					}

					if (!corpus2Option.HasValue())
					{
						app.Out.WriteLine("A file was not specified for the second corpus.");
						return 1;
					}

					if (!outputOption.HasValue())
					{
						app.Out.WriteLine("An output alignment file was not specified.");
						return 1;
					}

					if (corpus1Option.Values.Count != corpus2Option.Values.Count)
					{
						app.Out.WriteLine("The number of files in the two corpora are not the same.");
						return 1;
					}

					if (corpus1Option.Values.Count != outputOption.Values.Count)
					{
						app.Out.WriteLine("The number of output files does not match the number of corpus files.");
						return 1;
					}

					if (alignmentsOption.HasValue() && corpus1Option.Values.Count != alignmentsOption.Values.Count)
					{
						app.Out.WriteLine("The number of partial alignment files does not match the number of corpus files.");
						return 1;
					}

					var wordTokenizer = new WhitespaceTokenizer();
					var sourceCorpus = new DictionaryTextCorpus(corpus1Option.Values
						.Select((p, i) => new TextFileText(i.ToString(), p, wordTokenizer)));
					var targetCorpus = new DictionaryTextCorpus(corpus2Option.Values
						.Select((p, i) => new TextFileText(i.ToString(), p, wordTokenizer)));
					ITextAlignmentCorpus alignmentCorpus = null;
					if (alignmentsOption.HasValue())
					{
						alignmentCorpus = new DictionaryTextAlignmentCorpus(alignmentsOption.Values
							.Select((p, i) => new TextFileTextAlignmentCollection(i.ToString(), p)));
					}

					var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);
					using (var swaModel = new ThotSingleWordAlignmentModel())
					using (var invSwaModel = new ThotSingleWordAlignmentModel())
					{
						int segmentCount;
						app.Out.Write("Training... ");
						using (var progress = new ConsoleProgressBar(app.Out))
						{
							segmentCount = LoadModel(swaModel, parallelCorpus);
							Train(swaModel, progress);
							LoadModel(invSwaModel, parallelCorpus.Invert());
							Train(invSwaModel, progress, 5);
						}
						app.Out.WriteLine("done.");

						app.Out.Write("Aligning... ");
						var aligner = new SymmetrizationSegmentAligner(swaModel, invSwaModel);
						using (var progress = new ConsoleProgressBar(app.Out))
						{
							int i = 0, j = 0;
							foreach (ParallelText text in parallelCorpus.Texts)
							{
								using (var writer = new StreamWriter(File.Open(outputOption.Values[i], FileMode.Create)))
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
											writer.WriteLine(OutputAlignmentString(swaModel, invSwaModel, probOption.HasValue(),
												sourceTokens, targetTokens, matrix));
											j++;
											progress.Report((double) j / segmentCount);
										}
									}
								}
								i++;
							}
						}
						app.Out.WriteLine("done.");
					}

					return 0;
				});

			app.Execute(args);
		}

		private static int LoadModel(ThotSingleWordAlignmentModel swAlignModel, ParallelTextCorpus parallelCorpus)
		{
			int count = 0;
			foreach (ParallelTextSegment segment in parallelCorpus.Segments.Where(s => !s.IsEmpty))
			{
				string[] sourceTokens = segment.SourceSegment.Select(Preprocessors.Lowercase).ToArray();
				string[] targetTokens = segment.TargetSegment.Select(Preprocessors.Lowercase).ToArray();
				swAlignModel.AddSegmentPair(sourceTokens, targetTokens, segment.CreateAlignmentMatrix(true));
				count++;
			}
			return count;
		}

		private static void Train(ThotSingleWordAlignmentModel swAlignModel, IProgress<double> progress, int startStep = 0)
		{
			for (int i = 0; i < 5; i++)
			{
				swAlignModel.Train(1);
				progress.Report((double) (startStep + i + 1) / 10);
			}
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

			// all remaining target indices should be unaligned
			for (int i = 0; i < matrix.RowCount; i++)
			{
				if (targetIndices[i] == -2)
				{
					prev = i == 0 ? -1 : targetIndices[i - 1];
					targetIndices[i] = prev == -1 ? -1 : matrix.ColumnCount + prev;
				}
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