using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public abstract class TextBase : IText
	{
		protected TextBase(string id, string sortKey)
		{
			Id = id;
			SortKey = sortKey;
		}

		public string Id { get; }

		public string SortKey { get; }

		public virtual bool MissingRowsAllowed => true;

		public virtual int Count(bool includeEmpty = true)
		{
			return includeEmpty ? GetRows().Count() : GetRows().Count(r => !r.IsEmpty);
		}

		public abstract IEnumerable<TextRow> GetRows();

		protected TextRow CreateRow(string text, object segRef,
			bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
		{
			text = text.Trim();
			return new TextRow(segRef)
			{
				Segment = new[] { text },
				IsSentenceStart = isSentenceStart,
				IsInRange = isInRange,
				IsRangeStart = isRangeStart,
				IsEmpty = text.Length == 0
			};
		}

		protected TextRow CreateEmptyRow(object segRef, bool isInRange = false)
		{
			return new TextRow(segRef) { IsInRange = isInRange };
		}

		public IEnumerator<TextRow> GetEnumerator()
		{
			return GetRows().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
