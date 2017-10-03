using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public class UsfmFileTextCorpus : ITextCorpus
	{
		private readonly string _projectPath;
		private readonly UsfmStylesheet _stylesheet;
		private readonly Encoding _encoding;
		private readonly ITokenizer<string, int> _wordTokenizer;

		public UsfmFileTextCorpus(string stylesheetFileName, Encoding encoding, string projectPath,
			ITokenizer<string, int> wordTokenizer)
		{
			_projectPath = projectPath;
			_stylesheet = new UsfmStylesheet(stylesheetFileName);
			_encoding = encoding;
			_wordTokenizer = wordTokenizer;
		}

		public IEnumerable<IText> Texts
		{
			get
			{
				foreach (string sfmFileName in Directory.EnumerateFiles(_projectPath, "*.SFM"))
					yield return new UsfmFileText(_wordTokenizer, _stylesheet, _encoding, sfmFileName);
			}
		}

		public bool TryGetText(string id, out IText text)
		{
			string sfmFileName = Directory.EnumerateFiles(_projectPath, $"*{id}*.SFM").FirstOrDefault();
			if (sfmFileName != null)
			{
				text = new UsfmFileText(_wordTokenizer, _stylesheet, _encoding, sfmFileName);
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
