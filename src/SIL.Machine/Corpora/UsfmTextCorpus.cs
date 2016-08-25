using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIL.Machine.Corpora
{
	public class UsfmTextCorpus : ITextCorpus
	{
		private readonly string _projectPath;
		private readonly UsfmStylesheet _stylesheet;
		private readonly Encoding _encoding;

		public UsfmTextCorpus(string stylesheetFileName, Encoding encoding, string projectPath)
		{
			_projectPath = projectPath;
			_stylesheet = new UsfmStylesheet(stylesheetFileName);
			_encoding = encoding;
		}

		public IEnumerable<IText> Texts
		{
			get
			{
				foreach (string sfmFileName in Directory.EnumerateFiles(_projectPath, "*.SFM"))
					yield return new UsfmText(_stylesheet, _encoding, sfmFileName);
			}
		}

		public bool TryGetText(string id, out IText text)
		{
			string sfmFileName = Directory.EnumerateFiles(_projectPath, string.Format("{0}*.SFM", id)).FirstOrDefault();
			if (sfmFileName != null)
			{
				text = new UsfmText(_stylesheet, _encoding, sfmFileName);
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
