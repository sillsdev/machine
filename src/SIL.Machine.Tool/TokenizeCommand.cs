using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine
{
	public class TokenizeCommand : CommandBase
	{
		private readonly MonolingualCorpusCommandSpec _corpusSpec;
		private readonly CommandArgument _outputArgument;
		private readonly PreprocessCommandSpec _preprocessSpec;
		private readonly CommandOption _quietOption;

		public TokenizeCommand()
		{
			Name = "tokenize";
			Description = "Tokenizes a text corpus.";

			_corpusSpec = AddSpec(new MonolingualCorpusCommandSpec());
			_outputArgument = Argument("OUTPUT_FILE", "The output text file.").IsRequired();
			_preprocessSpec = AddSpec(new PreprocessCommandSpec { EscapeSpaces = true });
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
		{
			int code = await base.ExecuteCommandAsync(ct);
			if (code != 0)
				return code;

			if (!_quietOption.HasValue())
				Out.Write("Tokenizing... ");

			int corpusCount = Math.Min(_corpusSpec.MaxCorpusCount, _corpusSpec.Corpus.Count());
			int segmentCount = 0;
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (StreamWriter outputWriter = ToolHelpers.CreateStreamWriter(_outputArgument.Value))
			{
				ITextCorpusView corpus = _preprocessSpec.Preprocess(_corpusSpec.Corpus)
					.CapSize(_corpusSpec.MaxCorpusCount);
				foreach (TextCorpusRow row in corpus.GetRows())
				{
					outputWriter.WriteLine(row.Text);

					segmentCount++;
					progress?.Report(new ProgressStatus(segmentCount, corpusCount));
				}
			}

			if (!_quietOption.HasValue())
				Out.WriteLine("done.");

			Out.WriteLine($"# of Segments written: {segmentCount}");

			return 0;
		}
	}
}
