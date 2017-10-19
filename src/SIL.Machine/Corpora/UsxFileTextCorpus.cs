using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class UsxFileTextCorpus : ITextCorpus
	{
		private readonly string _projectPath;
		private readonly ITokenizer<string, int> _wordTokenizer;

		public UsxFileTextCorpus(ITokenizer<string, int> wordTokenizer, string projectPath)
		{
			_projectPath = projectPath;
			_wordTokenizer = wordTokenizer;
		}

		public IEnumerable<IText> Texts
		{
			get
			{
				foreach (string fileName in Directory.EnumerateFiles(_projectPath, "*.usx"))
					yield return new UsxFileText(_wordTokenizer, fileName);
			}
		}

		public bool TryGetText(string id, out IText text)
		{
			string fileName = Directory.EnumerateFiles(_projectPath, $"*{id}.usx").FirstOrDefault();
			if (fileName != null)
			{
				text = new UsxFileText(_wordTokenizer, fileName);
				return true;
			}

			text = null;
			return false;
		}

		public IText GetText(string id)
		{
			IText text;
			if (TryGetText(id, out text))
				return text;

			throw new ArgumentException("The specified identifier is not valid.", nameof(id));
		}
	}
}
