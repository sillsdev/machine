using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SmtResult
	{
		private readonly ReadOnlyList<string> _translation;
		private readonly ReadOnlyList<int> _sourceWordIndices; 
		private readonly ReadOnlyList<float> _wordConfidences; 

		internal SmtResult(IEnumerable<string> translation, IEnumerable<int> sourceWordIndices, IEnumerable<float> wordConfidences)
		{
			_translation = new ReadOnlyList<string>(translation.ToArray());
			_sourceWordIndices = new ReadOnlyList<int>(sourceWordIndices.ToArray());
			_wordConfidences = new ReadOnlyList<float>(wordConfidences.ToArray());
		}

		public IReadOnlyList<string> Translation
		{
			get { return _translation; }
		}

		public IReadOnlyList<int> SourceWordIndices
		{
			get { return _sourceWordIndices; }
		}

		public IReadOnlyList<float> WordConfidences
		{
			get { return _wordConfidences; }
		}
	}
}
