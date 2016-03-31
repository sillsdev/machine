using System;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SmtResult
	{
		private readonly ReadOnlyList<string> _translation;
		private readonly ReadOnlyList<float> _wordConfidences; 

		internal SmtResult(string translation, IEnumerable<float> wordConfidences)
		{
			_translation = new ReadOnlyList<string>(translation.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
			_wordConfidences = new ReadOnlyList<float>(wordConfidences.ToArray());
		}

		public IReadOnlyList<string> Translation
		{
			get { return _translation; }
		}

		public IReadOnlyList<float> WordConfidences
		{
			get { return _wordConfidences; }
		}
	}
}
