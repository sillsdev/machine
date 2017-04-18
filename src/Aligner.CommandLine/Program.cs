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
							AlignCorpus(parallelCorpus, aligner, outputOption.Values, segmentCount, probOption.HasValue(),
								progress);
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

		private static void AlignCorpus(ParallelTextCorpus parallelCorpus, ISegmentAligner aligner,
			IReadOnlyList<string> outputPaths, int segmentCount, bool includeProbs, IProgress<double> progress)
		{
			int i = 0, j = 0;
			foreach (ParallelText text in parallelCorpus.Texts)
			{
				using (var writer = new StreamWriter(File.Open(outputPaths[i], FileMode.Create)))
				{
					foreach (ParallelTextSegment segment in text.Segments)
					{
						if (segment.IsEmpty)
						{
							writer.WriteLine();
						}
						else
						{
							string[] sourceTokens = segment.SourceSegment.Select(Preprocessors.Lowercase).ToArray();
							string[] targetTokens = segment.TargetSegment.Select(Preprocessors.Lowercase).ToArray();
							WordAlignmentMatrix alignment = aligner.GetBestAlignment(sourceTokens, targetTokens,
								segment.CreateAlignmentMatrix(true));
							writer.WriteLine(OutputAlignmentString(alignment, aligner, sourceTokens, targetTokens,
								includeProbs));
							j++;
							progress.Report((double) j / segmentCount);
						}
					}
				}
				i++;
			}
		}

		private static string OutputAlignmentString(WordAlignmentMatrix matrix, ISegmentAligner aligner,
			IReadOnlyList<string> source, IReadOnlyList<string> target, bool includeProbs)
		{
			return string.Join(" ", Enumerable.Range(0, matrix.RowCount)
				.SelectMany(si => Enumerable.Range(0, matrix.ColumnCount), (si, ti) => (SourceIndex: si, TargetIndex: ti))
				.Where(t => matrix[t.SourceIndex, t.TargetIndex] == AlignmentType.Aligned)
				.Select(t => AlignedWordsString(t.SourceIndex, t.TargetIndex, aligner, source, target, includeProbs)));
		}

		private static string AlignedWordsString(int sourceIndex, int targetIndex, ISegmentAligner aligner,
			IReadOnlyList<string> source, IReadOnlyList<string> target, bool includeProbs)
		{
			if (includeProbs)
			{
				double prob = aligner.GetTranslationProbability(source[sourceIndex], target[targetIndex]);
				return $"{sourceIndex}-{targetIndex}:{prob:0.########}";
			}

			return $"{sourceIndex}-{targetIndex}";
		}
	}
}