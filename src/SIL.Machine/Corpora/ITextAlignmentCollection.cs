using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextAlignmentCollection
	{
		string Id { get; }

		string SortKey { get; }

		IEnumerable<TextAlignment> Alignments { get; }

		ITextAlignmentCollection Invert();
	}
}
