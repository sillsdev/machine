using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.DataStructures;
using SIL.Machine.Threading;
using SIL.Machine.Translation;

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
			_outputArgument = Argument("OUTPUT_PATH", "The output alignment file/directory (Pharaoh).")
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

			bool isOutputFile;
			if (ToolHelpers.IsDirectoryPath(_outputArgument.Value))
			{
				Directory.CreateDirectory(_outputArgument.Value);
				isOutputFile = false;
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_outputArgument.Value));
				isOutputFile = true;
			}

			List<IReadOnlyCollection<AlignedWordPair>> alignments = null;
			ParallelTextCorpus refParallelCorpus = null;
			if (_refOption.HasValue())
			{
				alignments = new List<IReadOnlyCollection<AlignedWordPair>>();
				ITextAlignmentCorpus refCorpus = ToolHelpers.CreateAlignmentsCorpus("text", _refOption.Value());
				refCorpus = _corpusSpec.FilterTextAlignmentCorpus(refCorpus);
				refParallelCorpus = new ParallelTextCorpus(_corpusSpec.SourceCorpus, _corpusSpec.TargetCorpus,
					refCorpus);
			}

			ITokenProcessor processor = _preprocessSpec.GetProcessor();

			int parallelCorpusCount = _corpusSpec.GetNonemptyParallelCorpusCount();
			SymmetrizationHeuristic symHeuristic = ToolHelpers.GetSymmetrizationHeuristic(_symHeuristicOption?.Value());

			if (!_quietOption.HasValue())
				Out.Write("Loading model... ");
			using (IWordAlignmentModel alignmentModel = _modelSpec.CreateAlignmentModel(symHeuristic: symHeuristic))
			{
				if (!_quietOption.HasValue())
				{
					Out.WriteLine("done.");
					Out.Write("Aligning... ");
				}
				var watch = Stopwatch.StartNew();
				using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
				using (StreamWriter writer = isOutputFile
					? ToolHelpers.CreateStreamWriter(_outputArgument.Value) : null)
				{
					int segmentCount = 0;
					progress?.Report(new ProgressStatus(segmentCount, parallelCorpusCount));
					foreach (ParallelText text in _corpusSpec.ParallelCorpus.Texts)
					{
						StreamWriter textWriter = isOutputFile ? writer
							: new StreamWriter(Path.Combine(_outputArgument.Value, text.Id.Trim('*') + ".txt"));
						try
						{
							var queue = new AsyncCollection<(long, string)>(100000);
							var writeTask = Task.Run(async () =>
							{
								int curIndex = 0;
								var completed = new PriorityQueue<long, string>();
								while (segmentCount < _corpusSpec.MaxCorpusCount)
								{
									IReadOnlyCollection<(long, string)> results = await queue.TakeAllAsync();
									if (results.Count > 0)
									{
										foreach ((long i, string alignmentStr) in results)
											completed.Enqueue(i, alignmentStr);
									}
									else
									{
										break;
									}

									while (!completed.IsEmpty && completed.Peek().Priority == curIndex)
									{
										string alignmentStr = completed.Dequeue().Item;
										writer.WriteLine(alignmentStr);
										if (alignmentStr != "")
										{
											alignments?.Add(AlignedWordPair.Parse(alignmentStr));
											segmentCount++;
											progress?.Report(new ProgressStatus(segmentCount, parallelCorpusCount));
											if (segmentCount == _corpusSpec.MaxCorpusCount)
												break;
										}
										curIndex++;
									}
								}
								queue.CompleteAdding();
							});

							Parallel.ForEach(Partitioner.Create(text.Segments), async (segment, state, i) =>
							{
								if (writeTask.IsCompleted)
								{
									state.Stop();
									return;
								}

								if (_modelSpec.IsSegmentInvalid(segment))
								{
									await queue.AddAsync((i, ""));
									writer.WriteLine();
								}
								else
								{
									IReadOnlyList<string> sourceSegment = processor.Process(segment.SourceSegment);
									IReadOnlyList<string> targetSegment = processor.Process(segment.TargetSegment);
									WordAlignmentMatrix alignment = alignmentModel.GetBestAlignment(sourceSegment,
										targetSegment, segment.CreateAlignmentMatrix());
									string alignmentStr = alignment.ToString(alignmentModel, sourceSegment,
										targetSegment, includeScores: _scoresOption.HasValue());
									if (!await queue.TryAddAsync((i, alignmentStr)))
										state.Stop();
								}
							});
							queue.CompleteAdding();
							await writeTask;

							if (segmentCount == _corpusSpec.MaxCorpusCount)
								break;
						}
						finally
						{
							if (!isOutputFile)
								textWriter.Close();
						}
					}
				}
				if (!_quietOption.HasValue())
					Out.WriteLine("done.");
				watch.Stop();

				Out.WriteLine($"Execution time: {watch.Elapsed:c}");
			}

			if (refParallelCorpus != null && alignments != null)
			{
				double aer = Evaluation.ComputeAer(alignments, refParallelCorpus.GetSegments()
					.Where(s => !s.IsEmpty).Select(s => s.AlignedWordPairs));
				(double fScore, double precision, double recall) = Evaluation.ComputeAlignmentFScore(alignments,
					refParallelCorpus.GetSegments().Where(s => !s.IsEmpty).Select(s => s.AlignedWordPairs));
				Out.WriteLine($"AER: {aer:0.0000}");
				Out.WriteLine($"F-Score: {fScore:0.0000}");
				Out.WriteLine($"Precision: {precision:0.0000}");
				Out.WriteLine($"Recall: {recall:0.0000}");
			}

			return 0;
		}
	}
}
