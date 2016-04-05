using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SegmentTranslator
	{
		public const float WordConfidenceThreshold = 0.03f;

		private readonly ISmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private readonly ReadOnlyList<string> _sourceSegment;
		private readonly List<string> _translation;
		private readonly ReadOnlyList<string> _readOnlyTranslation;
		private readonly List<int> _sourceWordIndices;
		private readonly ReadOnlyList<int> _readOnlySourceWordIndices; 
		private readonly List<float> _wordConfidences;
		private readonly ReadOnlyList<float> _readOnlyWordConfidences;
		private readonly List<string> _prefix;
		private readonly ReadOnlyList<string> _readOnlyPrefix; 
		private bool _isLastWordPartial;

		internal SegmentTranslator(ISmtSession smtSession, TransferEngine transferEngine, IEnumerable<string> segment)
		{
			_smtSession = smtSession;
			_transferEngine = transferEngine;
			_sourceSegment = new ReadOnlyList<string>(segment.ToArray());
			_prefix = new List<string>();
			_readOnlyPrefix = new ReadOnlyList<string>(_prefix);
			_translation = new List<string>();
			_readOnlyTranslation = new ReadOnlyList<string>(_translation);
			_sourceWordIndices = new List<int>();
			_readOnlySourceWordIndices = new ReadOnlyList<int>(_sourceWordIndices);
			_wordConfidences = new List<float>();
			_readOnlyWordConfidences = new ReadOnlyList<float>(_wordConfidences);
			_isLastWordPartial = true;
			ProcessResult(_smtSession.TranslateInteractively(_sourceSegment));
		}

		public IReadOnlyList<string> SourceSegment
		{
			get { return _sourceSegment; }
		}

		public IReadOnlyList<string> Prefix
		{
			get { return _readOnlyPrefix; }
		}

		public void SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			_prefix.Clear();
			_prefix.AddRange(prefix);
			_isLastWordPartial = isLastWordPartial;
			ProcessResult(_smtSession.SetPrefix(_prefix, _isLastWordPartial));
		}

		public void AddToPrefix(string addition, bool isWordPartial)
		{
			_prefix.Add(addition);
			_isLastWordPartial = isWordPartial;
			ProcessResult(_smtSession.AddToPrefix(addition.ToEnumerable(), _isLastWordPartial));
		}

		public void AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			string[] additionArray = addition.ToArray();
			_prefix.AddRange(additionArray);
			_isLastWordPartial = isLastWordPartial;
			ProcessResult(_smtSession.AddToPrefix(additionArray, _isLastWordPartial));
		}

		public bool IsLastWordPartial
		{
			get { return _isLastWordPartial; }
		}

		public IReadOnlyList<string> Translation
		{
			get { return _readOnlyTranslation; }
		}

		public IReadOnlyList<int> SourceWordIndices
		{
			get { return _readOnlySourceWordIndices; }
		}

		public IReadOnlyList<float> WordConfidences
		{
			get { return _readOnlyWordConfidences; }
		}

		public void Approve()
		{
			_smtSession.Train(_sourceSegment, _prefix);
		}

		private void ProcessResult(SmtResult result)
		{
			_translation.Clear();
			_sourceWordIndices.Clear();
			_wordConfidences.Clear();
			for (int i = 0; i < result.Translation.Count; i++)
			{
				float confidence;
				string targetWord;
				if (_transferEngine != null && result.WordConfidences[i] < WordConfidenceThreshold
					&& _transferEngine.TryTranslateWord(result.Translation[i], out targetWord))
				{
					confidence = 1.0f;
				}
				else
				{
					targetWord = result.Translation[i];
					confidence = result.WordConfidences[i];
				}
				_translation.Add(targetWord);
				_sourceWordIndices.Add(result.SourceWordIndices[i]);
				_wordConfidences.Add(confidence);
			}
		}
	}
}
