using System.Collections.Generic;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public class CorpusCommandSpecBase : ICommandSpec
	{
		private CommandOption _includeOption;
		private CommandOption _excludeOption;
		private CommandOption _maxCorpusSizeOption;
		private ISet<string> _includeTexts;
		private ISet<string> _excludeTexts;

		public int MaxCorpusCount { get; private set; } = int.MaxValue;

		public virtual void AddParameters(CommandBase command)
		{
			_includeOption = command.Option("-i|--include <TEXTS>",
				"The texts to include.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_excludeOption = command.Option("-e|--exclude <TEXTS>",
				"The texts to exclude.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_maxCorpusSizeOption = command.Option("-m|--max-size <SIZE>", "The maximum parallel corpus size.",
				CommandOptionType.SingleValue);
		}

		public virtual bool Validate(TextWriter outWriter)
		{
			if (_maxCorpusSizeOption.HasValue())
			{
				if (!int.TryParse(_maxCorpusSizeOption.Value(), out int maxCorpusSize) || maxCorpusSize <= 0)
				{
					outWriter.WriteLine("The specified maximum corpus size is invalid.");
					return false;
				}
				MaxCorpusCount = maxCorpusSize;
			}

			if (_includeOption.HasValue())
				_includeTexts = TranslatorHelpers.GetTexts(_includeOption.Values);

			if (_excludeOption.HasValue())
				_excludeTexts = TranslatorHelpers.GetTexts(_excludeOption.Values);

			return true;
		}

		public ITextCorpus FilterTextCorpus(ITextCorpus corpus)
		{
			if (_includeTexts != null || _excludeTexts != null)
				return new FilteredTextCorpus(corpus, text => FilterCorpus(text.Id));
			return corpus;
		}

		public ITextAlignmentCorpus FilterTextAlignmentCorpus(ITextAlignmentCorpus corpus)
		{
			if (_includeTexts != null || _excludeTexts != null)
				return new FilteredTextAlignmentCorpus(corpus, text => FilterCorpus(text.Id));
			return corpus;
		}

		protected bool FilterCorpus(string id)
		{
			if (_excludeTexts != null && _excludeTexts.Contains(id))
				return false;

			if (_includeTexts != null && _includeTexts.Contains(id))
				return true;

			return _includeTexts == null;
		}
	}
}
