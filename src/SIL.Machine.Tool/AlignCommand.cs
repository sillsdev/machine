using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine
{
	public class AlignCommand : CommandBase
	{
		private readonly AlignmentModelCommandSpec _modelSpec;
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandArgument _outputArgument;
		private readonly CommandOption _refOption;
		private readonly CommandOption _symHeuristicOption;
		private readonly CommandOption _scoresOption;
		private readonly PreprocessCommandSpec _preprocessSpec;
		private readonly CommandOption _quietOption;

		public AlignCommand()
		{
			Name = "align";
			Description = "Aligns parallel segments using a word alignment model.";

			_modelSpec = AddSpec(new AlignmentModelCommandSpec());
			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec());
			_outputArgument = Argument("OUTPUT_FILE", "The output alignment file (Pharaoh).")
				.IsRequired();
			_refOption = Option("-r|--reference <REF_PATH>",
				"The reference alignments corpus.\nIf specified, AER and F-Score will be computed for the generated alignments.",
				CommandOptionType.SingleValue);
			_symHeuristicOption = Option("-sh|--sym-heuristic <SYM_HEURISTIC>",
				$"The symmetrization heuristic.\nHeuristics: \"{ToolHelpers.Och}\" (default), \"{ToolHelpers.Union}\", \"{ToolHelpers.Intersection}\", \"{ToolHelpers.Grow}\", \"{ToolHelpers.GrowDiag}\", \"{ToolHelpers.GrowDiagFinal}\", \"{ToolHelpers.GrowDiagFinalAnd}\", \"{ToolHelpers.None}\".",
				CommandOptionType.SingleValue);
			_scoresOption = Option("-s|--scores", "Include scores in the output.", CommandOptionType.NoValue);
			_preprocessSpec = AddSpec(new PreprocessCommandSpec());
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
		{
			int code = await base.ExecuteCommandAsync(ct);
			if (code != 0)
				return code;

			if (!ToolHelpers.ValidateSymmetrizationHeuristicOption(_symHeuristicOption?.Value()))
			{
				Out.WriteLine("The specified symmetrization heuristic is invalid.");
				return 1;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(_outputArgument.Value));

			List<IReadOnlyCollection<AlignedWordPair>> alignments = null;
			IEnumerable<ParallelTextRow> refParallelCorpus = null;
			if (_refOption.HasValue())
			{
				alignments = new List<IReadOnlyCollection<AlignedWordPair>>();
				IAlignmentCorpus refCorpus = ToolHelpers.CreateAlignmentsCorpus("text",
					_refOption.Value());
				refCorpus = _corpusSpec.FilterAlignmentCorpus(refCorpus);
				refParallelCorpus = _corpusSpec.SourceCorpus
					.AlignRows(_corpusSpec.TargetCorpus, refCorpus)
					.Where(r => !r.IsEmpty);
			}

			int processorCount = Environment.ProcessorCount;

			SymmetrizationHeuristic symHeuristic = ToolHelpers.GetSymmetrizationHeuristic(_symHeuristicOption?.Value());

			if (!_quietOption.HasValue())
				Out.Write("Loading model... ");
			int stepCount = _quietOption.HasValue() ? 0
				: Math.Min(_corpusSpec.MaxCorpusCount, _corpusSpec.ParallelCorpus.Count(r => !r.IsEmpty));
			int curStep = 0;
			using (IWordAlignmentModel alignmentModel = _modelSpec.CreateAlignmentModel(symHeuristic: symHeuristic))
			{
				if (!_quietOption.HasValue())
				{
					Out.WriteLine("done.");
					Out.Write("Aligning... ");
				}
				var watch = Stopwatch.StartNew();
				using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
				using (StreamWriter writer = ToolHelpers.CreateStreamWriter(_outputArgument.Value))
				{
					int segmentCount = 0;
					progress?.Report(new ProgressStatus(curStep, stepCount));
					var alignBlock = new TransformBlock<ParallelTextRow, string>(row =>
					{
						if (row.IsEmpty)
							return "";
						return alignmentModel.GetAlignmentString(row, _scoresOption.HasValue());
					}, new ExecutionDataflowBlockOptions
					{
						MaxDegreeOfParallelism = processorCount - 1,
						BoundedCapacity = 100000
					});

					var batchWritesBlock = new BatchBlock<string>(10,
						new GroupingDataflowBlockOptions { BoundedCapacity = 100000 });

					var writeBlock = new ActionBlock<string[]>(async results =>
					{
						foreach (string alignmentStr in results)
						{
							await writer.WriteLineAsync(alignmentStr);
							if (alignmentStr != "")
							{
								alignments?.Add(AlignedWordPair.Parse(alignmentStr));
								curStep++;
								progress?.Report(new ProgressStatus(curStep, stepCount));
							}
						}
					}, new ExecutionDataflowBlockOptions { BoundedCapacity = 10000 });


					var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
					alignBlock.LinkTo(batchWritesBlock, linkOptions);
					batchWritesBlock.LinkTo(writeBlock, linkOptions);

					IEnumerable<ParallelTextRow> corpus = _preprocessSpec.Preprocess(_corpusSpec.ParallelCorpus);
					foreach (ParallelTextRow row in corpus)
					{
						await alignBlock.SendAsync(row);
						if (!row.IsEmpty)
						{
							segmentCount++;
							if (segmentCount == _corpusSpec.MaxCorpusCount)
								break;
						}
					}
					alignBlock.Complete();

					await writeBlock.Completion;
				}
				if (!_quietOption.HasValue())
					Out.WriteLine("done.");
				watch.Stop();

				Out.WriteLine($"Execution time: {watch.Elapsed:c}");
			}

			if (refParallelCorpus != null && alignments != null)
			{
				double aer = Evaluation.ComputeAer(alignments, refParallelCorpus.Select(s => s.AlignedWordPairs));
				(double fScore, double precision, double recall) = Evaluation.ComputeAlignmentFScore(alignments,
					refParallelCorpus.Select(s => s.AlignedWordPairs));
				Out.WriteLine($"AER: {aer:0.0000}");
				Out.WriteLine($"F-Score: {fScore:0.0000}");
				Out.WriteLine($"Precision: {precision:0.0000}");
				Out.WriteLine($"Recall: {recall:0.0000}");
			}

			return 0;
		}
	}
}
