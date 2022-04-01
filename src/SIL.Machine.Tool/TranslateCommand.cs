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
			_outputArgument = Argument("OUTPUT_FILE", "The output translation file (text).").IsRequired();
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

			Directory.CreateDirectory(Path.GetDirectoryName(_outputArgument.Value));

			List<IReadOnlyList<string>> translations = null;
			IParallelTextCorpus refParallelCorpus = null;
			if (_refOption.HasValue())
			{
				translations = new List<IReadOnlyList<string>>();
				ITextCorpus refCorpus = ToolHelpers.CreateTextCorpus(_refFormatOption.Value() ?? "text",
					_refOption.Value());
				refCorpus = _corpusSpec.FilterTextCorpus(refCorpus);
				ITokenizer<string, int, string> refWordTokenizer = ToolHelpers.CreateWordTokenizer(
					_refWordTokenizerOption.Value() ?? "whitespace");
				refCorpus = refCorpus.Tokenize(refWordTokenizer);
				refParallelCorpus = _corpusSpec.Corpus
					.AlignRows(refCorpus)
					.Where(row => row.SourceSegment.Count > 0
						&& row.SourceSegment.Count <= TranslationConstants.MaxSegmentLength);
				refParallelCorpus = _preprocessSpec.Preprocess(refParallelCorpus);
			}

			IDetokenizer<string, string> refWordDetokenizer = ToolHelpers.CreateWordDetokenizer(
				_refWordTokenizerOption.Value() ?? "whitespace");

			if (!_quietOption.HasValue())
				Out.Write("Loading model... ");
			int corpusCount = _corpusSpec.Corpus.Count(IsValid);
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
				using (StreamWriter writer = ToolHelpers.CreateStreamWriter(_outputArgument.Value))
				{
					ITextCorpus corpus = _preprocessSpec.Preprocess(_corpusSpec.Corpus);
					progress?.Report(new ProgressStatus(segmentCount, corpusCount));
					foreach (TextRow row in corpus)
					{
						if (IsValid(row))
						{
							TranslationResult translateResult = engine.Translate(row.Segment);
							translations?.Add(translateResult.TargetSegment);
							string translation = refWordDetokenizer.Detokenize(translateResult.TargetSegment);
							writer.WriteLine(translation);

							segmentCount++;
							progress?.Report(new ProgressStatus(segmentCount, corpusCount));
							if (segmentCount == corpusCount)
								break;
						}
						else
						{
							writer.WriteLine();
						}
					}
				}
				if (!_quietOption.HasValue())
					Out.WriteLine("done.");
			}


			if (refParallelCorpus != null && translations != null)
			{
				double bleu = Evaluation.ComputeBleu(translations, refParallelCorpus.Select(r => r.TargetSegment));
				Out.WriteLine($"BLEU: {bleu * 100:0.00}");
			}

			return 0;
		}

		private static bool IsValid(TextRow row)
		{
			return !row.IsEmpty && row.Segment.Count <= TranslationConstants.MaxSegmentLength;
		}
	}
}
