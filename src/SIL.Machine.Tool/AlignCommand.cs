using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine
{
	public class AlignCommand : CommandBase
	{
		private readonly AlignmentModelCommandSpec _modelSpec;
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandArgument _outputArgument;
		private readonly CommandOption _refOption;
		private readonly CommandOption _probOption;
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
			_probOption = Option("-p|--probabilities", "Include probabilities in the output.",
				CommandOptionType.NoValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

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

			int parallelCorpusCount = _corpusSpec.GetNonemptyParallelCorpusCount();

			if (!_quietOption.HasValue())
				Out.Write("Aligning... ");
			using (var progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (IWordAlignmentModel alignmentModel = CreateAlignmentModel())
			using (StreamWriter writer = isOutputFile ? new StreamWriter(_outputArgument.Value) : null)
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
							if (IsSegmentInvalid(segment))
							{
								writer.WriteLine();
							}
							else
							{
								IReadOnlyList<string> sourceSegment = TokenProcessors.Lowercase
									.Process(segment.SourceSegment);
								IReadOnlyList<string> targetSegment = TokenProcessors.Lowercase
									.Process(segment.TargetSegment);
								WordAlignmentMatrix alignment = alignmentModel.GetBestAlignment(sourceSegment,
									targetSegment, segment.CreateAlignmentMatrix());
								alignments?.Add(alignment.GetAlignedWordPairs());
								writer.WriteLine(alignment.ToString(alignmentModel, sourceSegment, targetSegment,
									_probOption.HasValue()));
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

		private IWordAlignmentModel CreateAlignmentModel()
		{
			if (_modelSpec.ModelFactory != null)
				return _modelSpec.ModelFactory.CreateModel(_modelSpec.ModelPath);

			switch (_modelSpec.ModelType)
			{
				case "hmm":
					return CreateThotAlignmentModel<HmmThotWordAlignmentModel>();
				case "ibm1":
					return CreateThotAlignmentModel<Ibm1ThotWordAlignmentModel>();
				case "ibm2":
					return CreateThotAlignmentModel<Ibm2ThotWordAlignmentModel>();
				case "smt":
					string modelCfgFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelSpec.ModelPath);
					return new ThotSmtWordAlignmentModel(modelCfgFileName);
			}
			throw new InvalidOperationException("An invalid alignment model type was specified.");
		}

		private IWordAlignmentModel CreateThotAlignmentModel<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string modelPath = _modelSpec.ModelPath;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");

			var directModel = new TAlignModel();
			directModel.Load(modelPath + "_invswm");

			var inverseModel = new TAlignModel();
			inverseModel.Load(modelPath + "_swm");

			return new SymmetrizedWordAlignmentModel(directModel, inverseModel);
		}

		private bool IsSegmentInvalid(ParallelTextSegment segment)
		{
			return segment.IsEmpty || (_modelSpec.ModelType == "smt"
				&& segment.SourceSegment.Count > TranslationConstants.MaxSegmentLength);
		}
	}
}
