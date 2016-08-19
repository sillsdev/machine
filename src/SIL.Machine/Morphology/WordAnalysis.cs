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
		private readonly ReadOnlyList<IMorpheme> _morphemes;
		private readonly int _rootMorphemeIndex;
		private readonly string _category;

		public WordAnalysis(IEnumerable<IMorpheme> morphemes, int rootMorphemeIndex, string category)
		{
			_morphemes = new ReadOnlyList<IMorpheme>(morphemes.ToArray());
			_rootMorphemeIndex = rootMorphemeIndex;
			_category = category;
		}

		/// <summary>
		/// Gets all of the morphemes in the order in which they occur in the word.
		/// </summary>
		public IReadOnlyList<IMorpheme> Morphemes
		{
			get { return _morphemes; }
		}

		/// <summary>
		/// Gets the root morpheme.
		/// </summary>
		public int RootMorphemeIndex
		{
			get { return _rootMorphemeIndex; }
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
			return other != null && _morphemes.Select(m => m.Id).SequenceEqual(other._morphemes.Select(m => m.Id))
				&& _rootMorphemeIndex == other._rootMorphemeIndex && _category == other._category;
		}

		public override int GetHashCode()
		{
			int code = 23;
			code += code * 31 + _morphemes.Select(m => m.Id).GetSequenceHashCode();
			code += code * 31 + _rootMorphemeIndex.GetHashCode();
			code += code * 31 + (_category == null ? 0 : _category.GetHashCode());
			return code;
		}

		public override string ToString()
		{
			return string.Format("[{0}]", string.Join(" ", _morphemes.Select((m, i) => i == _rootMorphemeIndex ? "*" + m : m.ToString())));
		}
	}
}
