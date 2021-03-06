﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
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

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
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
							foreach (ParallelTextSegment segment in text.Segments)
							{
								if (_modelSpec.IsSegmentInvalid(segment))
								{
									writer.WriteLine();
								}
								else
								{
									IReadOnlyList<string> sourceSegment = processor.Process(segment.SourceSegment);
									IReadOnlyList<string> targetSegment = processor.Process(segment.TargetSegment);
									WordAlignmentMatrix alignment = alignmentModel.GetBestAlignment(sourceSegment,
										targetSegment, segment.CreateAlignmentMatrix());
									alignments?.Add(alignment.GetAlignedWordPairs());
									writer.WriteLine(alignment.ToString(alignmentModel, sourceSegment, targetSegment,
										includeScores: _scoresOption.HasValue()));
									segmentCount++;
									progress?.Report(new ProgressStatus(segmentCount, parallelCorpusCount));
									if (segmentCount == _corpusSpec.MaxCorpusCount)
										break;
								}
							}
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
