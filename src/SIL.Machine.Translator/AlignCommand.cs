using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Paratext;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class AlignCommand : CommandBase
	{
		private readonly AlignmentModelCommandSpec _modelSpec;
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandArgument _outputArgument;
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
			if (TranslatorHelpers.IsDirectoryPath(_outputArgument.Value))
			{
				Directory.CreateDirectory(_outputArgument.Value);
				isOutputFile = false;
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_outputArgument.Value));
				isOutputFile = true;
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
							if (segment.IsEmpty)
							{
								writer.WriteLine();
							}
							else
							{
								writer.WriteLine(alignmentModel.GetAlignmentString(segment, _probOption.HasValue(),
									TokenProcessors.Lowercase, TokenProcessors.Lowercase));
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

			return 0;
		}

		private IWordAlignmentModel CreateAlignmentModel()
		{
			switch (_modelSpec.ModelType)
			{
				case "hmm":
					return CreateThotAlignmentModel<HmmThotWordAlignmentModel>();
				case "ibm1":
					return CreateThotAlignmentModel<Ibm1ThotWordAlignmentModel>();
				case "ibm2":
					return CreateThotAlignmentModel<Ibm2ThotWordAlignmentModel>();
				case "pt":
					return new ParatextWordAlignmentModel(_modelSpec.ModelPath);
			}
			throw new InvalidOperationException("An invalid alignment model type was specified.");
		}

		private IWordAlignmentModel CreateThotAlignmentModel<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			var directModel = new TAlignModel();
			directModel.Load(_modelSpec.ModelPath + "_invswm");

			var inverseModel = new TAlignModel();
			inverseModel.Load(_modelSpec.ModelPath + "_swm");

			return new SymmetrizedWordAlignmentModel(directModel, inverseModel);
		}
	}
}
