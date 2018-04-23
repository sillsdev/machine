using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class FilteredText : IText
	{
		private readonly IText _text;
		private readonly Func<TextSegment, bool> _filter;

		public FilteredText(IText text, Func<TextSegment, bool> filter)
		{
			_text = text;
			_filter = filter;
		}

		public string Id => _text.Id;

		public IEnumerable<TextSegment> Segments => _text.Segments.Where(_filter);
	}
}
