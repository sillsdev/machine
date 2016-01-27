using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class ThotResult
	{
		private readonly string _translation;
		private readonly ReadOnlyCollection<float> _wordConfidences; 

		internal ThotResult(string translation, IEnumerable<float> wordConfidences)
		{
			_translation = translation;
			_wordConfidences = new ReadOnlyCollection<float>(wordConfidences.ToList());
		}

		public string Translation
		{
			get { return _translation; }
		}

		public ReadOnlyCollection<float> WordConfidences
		{
			get { return _wordConfidences; }
		}

		public int WordCount
		{
			get { return _wordConfidences.Count; }
		}
	}
}
