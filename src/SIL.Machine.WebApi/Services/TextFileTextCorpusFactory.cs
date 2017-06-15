using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Options;

namespace SIL.Machine.WebApi.Services
{
	public class TextFileTextCorpusFactory : ITextCorpusFactory
	{
		private readonly string _textFileDir;

		public TextFileTextCorpusFactory(IOptions<TextFileTextCorpusOptions> options)
		{
			_textFileDir = options.Value.TextFileDir;
		}

		public ITextCorpus Create(IEnumerable<string> projects, ITokenizer<string, int> wordTokenizer, TextCorpusType type)
		{
			var texts = new List<IText>();
			foreach (string projectId in projects)
			{
				string dir = null;
				switch (type)
				{
					case TextCorpusType.Source:
						dir = "source";
						break;
					case TextCorpusType.Target:
						dir = "target";
						break;
				}

				foreach (string file in Directory.EnumerateFiles(Path.Combine(_textFileDir, projectId, dir), "*.txt"))
					texts.Add(new TextFileText($"{projectId}_{Path.GetFileNameWithoutExtension(file)}", file, wordTokenizer));
			}

			return new DictionaryTextCorpus(texts);
		}
	}
}
