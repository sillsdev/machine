using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Translation
{
	public class WordAnalysis
	{
		private readonly string _category;
		private readonly string _gloss;
		private readonly ReadOnlyList<Morpheme> _morphemes; 

		public WordAnalysis(IEnumerable<Morpheme> morphemes, string category, string gloss)
		{
			_category = category;
			_gloss = gloss;
			_morphemes = new ReadOnlyList<Morpheme>(morphemes.ToArray());
		}

		public IReadOnlyList<Morpheme> Morphemes
		{
			get { return _morphemes; }
		}

		public string Category
		{
			get { return _category; }
		}

		public string Gloss
		{
			get { return _gloss; }
		}
	}
}
