using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class MultipleText : IText
	{
		private readonly string _corpusKey;
		private readonly IText _text;

		public MultipleText(string corpusKey, IText text)
		{
			_corpusKey = corpusKey;
			_text = text;
		}

		public string Id => $"{_corpusKey}_{_text.Id}";

		public string SortKey => $"{_corpusKey}_{_text.SortKey}";

		public IEnumerable<TextCorpusRow> GetRows()
		{
			return _text.GetRows();
		}
	}
}
