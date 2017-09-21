using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Server.Options;

namespace SIL.Machine.WebApi.Server.Services
{
	public class TextFileTextCorpusFactory : ITextCorpusFactory
	{
		private readonly string _textFileDir;

		public TextFileTextCorpusFactory(IOptions<TextFileTextCorpusOptions> options)
		{
			_textFileDir = options.Value.TextFileDir;
		}

		public ITextCorpus Create(IEnumerable<string> projects, TextCorpusType type)
		{
			var wordTokenizer = new LatinWordTokenizer();
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
				{
					var text = new TextFileText($"{projectId}_{Path.GetFileNameWithoutExtension(file)}", file,
						wordTokenizer);
					texts.Add(text);
				}
			}

			return new DictionaryTextCorpus(texts);
		}
	}
}
