using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using SIL.Console;
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
				Description = "Aligns words in a parallel text corpus."
			};
			app.HelpOption("-?|-h|--help");

			CommandOption corpus1Option = app.Option("-c1|--corpus1 <path>", "The first corpus file(s).", CommandOptionType.MultipleValue);
			CommandOption corpus2Option = app.Option("-c2|--corpus2 <path>", "The second corpus file(s).", CommandOptionType.MultipleValue);
			CommandOption alignmentsOption = app.Option("-a|--alignments <path>", "The partial alignment file(s).", CommandOptionType.MultipleValue);
			CommandOption outputOption = app.Option("-o|--output <path>", "The output alignment file(s).", CommandOptionType.MultipleValue);

			app.OnExecute(() =>
				{
					if (!corpus1Option.HasValue())
					{
						app.Out.WriteLine("A first corpus file was not specified.");
						return 1;
					}

					if (!corpus2Option.HasValue())
					{
						app.Out.WriteLine("A second corpus file was not specified.");
						return 1;
					}

					if (!outputOption.HasValue())
					{
						app.Out.WriteLine("An output alignment file was not specified.");
						return 1;
					}

					if (corpus1Option.Values.Count != corpus2Option.Values.Count)
					{
						app.Out.WriteLine("The number of corpus files are not the same.");
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
					var sourceCorpus = new DictionaryTextCorpus(corpus1Option.Values.Select((p, i) => new TextFileText(i.ToString(), p, wordTokenizer)));
					var targetCorpus = new DictionaryTextCorpus(corpus2Option.Values.Select((p, i) => new TextFileText(i.ToString(), p, wordTokenizer)));
					ITextAlignmentCorpus alignmentCorpus = null;
					if (alignmentsOption.HasValue())
						alignmentCorpus = new DictionaryTextAlignmentCorpus(alignmentsOption.Values.Select((p, i) => new TextFileTextAlignmentCollection(i.ToString(), p)));

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
						using (var progress = new ConsoleProgressBar(app.Out))
						{
							var aligner = new SymmetrizationSegmentAligner(swaModel, invSwaModel);
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
											string[] sourceTokens = segment.SourceSegment.Select(Preprocessors.Lowercase).ToArray();
											string[] targetTokens = segment.TargetSegment.Select(Preprocessors.Lowercase).ToArray();
											WordAlignmentMatrix alignment = aligner.GetBestAlignment(sourceTokens, targetTokens, segment.CreateAlignmentMatrix(true));
											writer.WriteLine(alignment.ToString());
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
	}
}