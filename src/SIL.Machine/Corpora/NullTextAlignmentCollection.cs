using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class NullTextAlignmentCollection : ITextAlignmentCollection
	{
		public NullTextAlignmentCollection(string id, string sortKey)
		{
			Id = id;
			SortKey = sortKey;
		}

		public string Id { get; }

		public string SortKey { get; }

		public IEnumerable<TextAlignment> Alignments => Enumerable.Empty<TextAlignment>();

		public ITextAlignmentCollection Invert()
		{
			return this;
		}
	}
}
