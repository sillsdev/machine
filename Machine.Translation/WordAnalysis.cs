using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	/// <summary>
	/// This class represents a word analysis.
	/// </summary>
	public class WordAnalysis : IEquatable<WordAnalysis>
	{
		private readonly ReadOnlyList<MorphemeInfo> _morphemes;
		private readonly string _category;

		public WordAnalysis(IEnumerable<MorphemeInfo> morphemes, string category)
		{
			_morphemes = new ReadOnlyList<MorphemeInfo>(morphemes.ToArray());
			_category = category;
		}

		/// <summary>
		/// Gets all of the morphemes in the word analysis. The order of the morphemes is
		/// important and depends on what the source analyzer outputs and what the target
		/// generator expects.
		/// </summary>
		public IReadOnlyList<MorphemeInfo> Morphemes
		{
			get { return _morphemes; }
		}

		/// <summary>
		/// Gets the category or part of speech.
		/// </summary>
		public string Category
		{
			get { return _category; }
		}

		public override bool Equals(object obj)
		{
			var other = obj as WordAnalysis;
			return other != null && Equals(other);
		}

		public bool Equals(WordAnalysis other)
		{
			return other != null && _morphemes.SequenceEqual(other._morphemes) && _category == other._category;
		}

		public override int GetHashCode()
		{
			int code = 23;
			code += code * 31 + _morphemes.GetSequenceHashCode();
			code += code * 31 + (_category == null ? 0 : _category.GetHashCode());
			return code;
		}

		public override string ToString()
		{
			return string.Format("[{0}]", string.Join(" ", _morphemes));
		}
	}
}
