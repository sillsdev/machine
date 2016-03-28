using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SegmentTranslator : DisposableBase
	{
		private readonly SmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private readonly ReadOnlyList<string> _segment;
		private readonly List<string> _currentTranslation;
		private readonly ReadOnlyList<string> _readOnlyCurrentTranslation; 
		private readonly ObservableList<string> _prefix;

		internal SegmentTranslator(SmtSession smtSession, TransferEngine transferEngine, IEnumerable<string> segment)
		{
			_smtSession = smtSession;
			_transferEngine = transferEngine;
			_segment = new ReadOnlyList<string>(segment.ToArray());
			_prefix = new ObservableList<string>();
			_prefix.CollectionChanged += PrefixChanged;
			_currentTranslation = new List<string>();
			_readOnlyCurrentTranslation = new ReadOnlyList<string>(_currentTranslation);
			ProcessResult(_smtSession.TranslateInteractively(_segment));
		}

		private void PrefixChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			ProcessResult(_smtSession.SetPrefix(_prefix));
		}

		public IReadOnlyList<string> Segment
		{
			get { return _segment; }
		}

		public IList<string> Prefix
		{
			get { return _prefix; }
		}

		public IReadOnlyList<string> CurrentTranslation
		{
			get { return _readOnlyCurrentTranslation; }
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Train(_segment, _currentTranslation);
		}

		private void ProcessResult(SmtResult result)
		{
			_currentTranslation.Clear();
			for (int i = 0; i < result.Translation.Count; i++)
			{
				string targetWord;
				if (_transferEngine == null || result.WordConfidences[i] >= 0.001 || !_transferEngine.TryTranslateWord(result.Translation[i], out targetWord))
					targetWord = result.Translation[i];
				_currentTranslation.Add(targetWord);
			}
		}
	}
}
