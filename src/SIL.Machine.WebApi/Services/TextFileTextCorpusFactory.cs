using System.Collections.Generic;
using System.IO;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public class TextFileTextCorpusFactory : ITextCorpusFactory
	{
		public ITextCorpus Create(IEnumerable<Project> projects, ITokenizer<string, int> wordTokenizer, TextCorpusType type)
		{
			var texts = new List<IText>();
			foreach (Project project in projects)
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

				foreach (string file in Directory.EnumerateFiles(Path.Combine(project.Directory, dir), "*.txt"))
					texts.Add(new TextFileText($"{project.Id}_{Path.GetFileNameWithoutExtension(file)}", file, wordTokenizer));
			}

			return new DictionaryTextCorpus(texts);
		}
	}
}
