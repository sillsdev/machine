using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine
{
	public class TranslateCommand : CommandBase
	{
		private readonly TranslationModelCommandSpec _modelSpec;
		private readonly MonolingualCorpusCommandSpec _corpusSpec;
		private readonly CommandArgument _outputArgument;
		private readonly CommandOption _refOption;
		private readonly CommandOption _refFormatOption;
		private readonly CommandOption _refWordTokenizerOption;
		private readonly PreprocessCommandSpec _preprocessSpec;
		private readonly CommandOption _quietOption;

		public TranslateCommand()
		{
			Name = "translate";
			Description = "Translates segments using a translation model.";

			_modelSpec = AddSpec(new TranslationModelCommandSpec());
			_corpusSpec = AddSpec(new MonolingualCorpusCommandSpec());
			_outputArgument = Argument("OUTPUT_PATH", "The output translation file/directory (text).").IsRequired();
			_refOption = Option("-r|--reference <REF_PATH>",
				"The reference corpus.\nIf specified, BLEU will be computed for the generated translations.",
				CommandOptionType.SingleValue);
			_refFormatOption = Option("-rf|--reference-format <CORPUS_FORMAT>",
				"The reference corpus format.\nFormats: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
				CommandOptionType.SingleValue);
			_refWordTokenizerOption = Option("-rt|--ref-tokenizer <TOKENIZER>",
				$"The reference word tokenizer.\nTypes: \"whitespace\" (default), \"latin\", \"zwsp\".",
				CommandOptionType.SingleValue);
			_preprocessSpec = AddSpec(new PreprocessCommandSpec());
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
		{
			int result = await base.ExecuteCommandAsync(ct);
			if (result != 0)
				return result;

			if (!ToolHelpers.ValidateCorpusFormatOption(_refFormatOption.Value()))
			{
				Out.WriteLine("The specified reference corpus format is invalid.");
				return 1;
			}

			if (!ToolHelpers.ValidateWordTokenizerOption(_refWordTokenizerOption.Value()))
			{
				Out.WriteLine("The specified reference word tokenizer is invalid.");
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

			List<IReadOnlyList<string>> translations = null;
			ParallelTextCorpus refParallelCorpus = null;
			if (_refOption.HasValue())
			{
				translations = new List<IReadOnlyList<string>>();
				ITokenizer<string, int, string> refWordTokenizer = ToolHelpers.CreateWordTokenizer(
					_refWordTokenizerOption.Value() ?? "whitespace");
				ITextCorpus refCorpus = ToolHelpers.CreateTextCorpus(refWordTokenizer,
					_refFormatOption.Value() ?? "text", _refOption.Value());
				refCorpus = _corpusSpec.FilterTextCorpus(refCorpus);
				refParallelCorpus = new ParallelTextCorpus(_corpusSpec.Corpus, refCorpus);
			}

			IDetokenizer<string, string> refWordDetokenizer = ToolHelpers.CreateWordDetokenizer(
				_refWordTokenizerOption.Value() ?? "whitespace");

			if (!_quietOption.HasValue())
				Out.Write("Loading model... ");
			int corpusCount = _corpusSpec.GetNonemptyCorpusCount();
			var truecaser = new TransferTruecaser();
			ITokenProcessor processor = _preprocessSpec.GetProcessor();
			int segmentCount = 0;
			using (ITranslationModel model = _modelSpec.CreateModel())
			using (ITranslationEngine engine = model.CreateEngine())
			{
				if (!_quietOption.HasValue())
				{
					Out.WriteLine("done.");
					Out.Write("Translating... ");
				}
				using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
				using (StreamWriter writer = isOutputFile
					? ToolHelpers.CreateStreamWriter(_outputArgument.Value) : null)
				{
					progress?.Report(new ProgressStatus(segmentCount, corpusCount));
					foreach (IText text in _corpusSpec.Corpus.Texts)
					{
						StreamWriter textWriter = isOutputFile ? writer
							: new StreamWriter(Path.Combine(_outputArgument.Value, text.Id.Trim('*') + ".txt"));
						try
						{
							foreach (TextSegment segment in text.GetSegments())
							{
								if (segment.IsEmpty || segment.Segment.Count > TranslationConstants.MaxSegmentLength)
								{
									textWriter.WriteLine();
								}
								else
								{
									TranslationResult translateResult = engine.Translate(
										processor.Process(segment.Segment));
									translations?.Add(translateResult.TargetSegment);
									translateResult = truecaser.Truecase(segment.Segment, translateResult);
									string translation = refWordDetokenizer.Detokenize(translateResult.TargetSegment);
									textWriter.WriteLine(translation);

									segmentCount++;
									progress?.Report(new ProgressStatus(segmentCount, corpusCount));
									if (segmentCount == corpusCount)
										break;
								}
							}
							if (segmentCount == corpusCount)
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


			if (refParallelCorpus != null && translations != null)
			{
				double bleu = Evaluation.ComputeBleu(translations, refParallelCorpus.GetSegments()
					.Where(s => s.SourceSegment.Count > 0
						&& s.SourceSegment.Count <= TranslationConstants.MaxSegmentLength)
					.Select(s => processor.Process(s.TargetSegment)));
				Out.WriteLine($"BLEU: {bleu * 100:0.00}");
			}

			return 0;
		}
	}
}
