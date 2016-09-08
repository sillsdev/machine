using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology
{
	/// <summary>
	/// This class represents a word analysis.
	/// </summary>
	public class WordAnalysis : IEquatable<WordAnalysis>
	{
		public WordAnalysis()
			: this(Enumerable.Empty<IMorpheme>(), -1, null)
		{
		}

		public WordAnalysis(IEnumerable<IMorpheme> morphemes, int rootMorphemeIndex, string category)
		{
			Morphemes = new ReadOnlyList<IMorpheme>(morphemes.ToArray());
			RootMorphemeIndex = rootMorphemeIndex;
			Category = category;
		}

		/// <summary>
		/// Gets all of the morphemes in the order in which they occur in the word.
		/// </summary>
		public IReadOnlyList<IMorpheme> Morphemes { get; }

		/// <summary>
		/// Gets the root morpheme.
		/// </summary>
		public int RootMorphemeIndex { get; }

		/// <summary>
		/// Gets the category or part of speech.
		/// </summary>
		public string Category { get; }

		public bool IsEmpty => Morphemes.Count == 0;

		public override bool Equals(object obj)
		{
			var other = obj as WordAnalysis;
			return other != null && Equals(other);
		}

		public bool Equals(WordAnalysis other)
		{
			return other != null && Morphemes.Select(m => m.Id).SequenceEqual(other.Morphemes.Select(m => m.Id))
				&& RootMorphemeIndex == other.RootMorphemeIndex && Category == other.Category;
		}

		public override int GetHashCode()
		{
			int code = 23;
			code += code * 31 + Morphemes.Select(m => m.Id).GetSequenceHashCode();
			code += code * 31 + RootMorphemeIndex.GetHashCode();
			code += code * 31 + Category?.GetHashCode() ?? 0;
			return code;
		}

		public override string ToString()
		{
			return string.Format("[{0}]", string.Join(" ", Morphemes.Select((m, i) => i == RootMorphemeIndex ? "*" + m : m.ToString())));
		}
	}
}
