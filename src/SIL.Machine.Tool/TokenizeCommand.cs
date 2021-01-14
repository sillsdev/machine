using System.Collections.Generic;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class TokenizeCommand : CommandBase
	{
		private readonly MonolingualCorpusCommandSpec _corpusSpec;
		private readonly CommandArgument _outputArgument;
		private readonly CommandOption _lowercaseOption;
		private readonly CommandOption _quietOption;

		public TokenizeCommand()
		{
			Name = "tokenize";
			Description = "Tokenizes a text corpus.";

			_corpusSpec = AddSpec(new MonolingualCorpusCommandSpec());
			_outputArgument = Argument("OUTPUT_FILE", "The output text file.").IsRequired();
			_lowercaseOption = Option("-l|--lowercase", "Convert text to lowercase.", CommandOptionType.NoValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!_quietOption.HasValue())
				Out.Write("Tokenizing... ");
			var processorPipeline = new List<ITokenProcessor>
			{
				TokenProcessors.Normalize,
				TokenProcessors.EscapeSpaces
			};
			if (_lowercaseOption.HasValue())
				processorPipeline.Add(TokenProcessors.Lowercase);
			ITokenProcessor processor = TokenProcessors.Pipeline(processorPipeline);
			int corpusCount = _corpusSpec.GetCorpusCount();
			int segmentCount = 0;
			var utf8Encoding = new UTF8Encoding(false);
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (var outputWriter = new StreamWriter(_outputArgument.Value, false, utf8Encoding))
			{
				foreach (TextSegment segment in _corpusSpec.Corpus.GetSegments())
				{
					outputWriter.WriteLine(string.Join(" ", processor.Process(segment.Segment)));

					segmentCount++;
					progress?.Report(new ProgressStatus(segmentCount, corpusCount));
					if (segmentCount == _corpusSpec.MaxCorpusCount)
						break;
				}
			}

			if (!_quietOption.HasValue())
				Out.WriteLine("done.");

			Out.WriteLine($"# of Segments written: {segmentCount}");

			return 0;
		}
	}
}
