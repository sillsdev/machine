using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Eto.Forms;
using GalaSoft.MvvmLight;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.TestApp
{
	public class TextViewModel : ViewModelBase, IChangeTracking
	{
		private readonly HybridTranslationEngine _engine;
		private readonly ITokenizer<string, int> _tokenizer;
		private readonly string _sourceFileName;
		private readonly string _targetFileName;

		private string _sourceText;
		private string _targetText;
		private string _sourceSegment;
		private string _targetSegment;
		private int _currentTargetSegmentIndex;
		private readonly RelayCommand<object> _goToNextSegmentCommand;
		private readonly RelayCommand<object> _goToPrevSegmentCommand;
		private readonly RelayCommand<object> _approveSegmentCommand; 
		private readonly RelayCommand<object> _applyAllSuggestionsCommand; 
		private readonly List<Segment> _sourceSegments;
		private readonly List<Segment> _targetSegments;
		private int _currentSegment = -1;
		private readonly HashSet<int> _paragraphs;
		private readonly BulkObservableList<SuggestionViewModel> _suggestions;
		private readonly List<string> _sourceSegmentWords;
		private readonly BulkObservableList<AlignedWordViewModel> _alignedSourceWords;
		private Range<int>? _currentSourceSegmentRange;
		private Range<int>? _currentTargetSegmentRange; 
		private readonly RelayCommand<int> _selectSourceSegmentCommand;
		private readonly RelayCommand<int> _selectTargetSegmentCommand;
		private readonly BulkObservableList<Range<int>> _unapprovedTargetSegmentRanges;
		private double _confidenceThreshold;
		private bool _isChanged;
		private bool _isActive;
		private bool _isTranslating;
		private HybridInteractiveTranslationSession _curSession;

		public TextViewModel(ITokenizer<string, int> tokenizer, string name, string sourceFileName, string targetFileName, HybridTranslationEngine engine)
		{
			Name = name;
			_sourceFileName = sourceFileName;
			_targetFileName = targetFileName;
			_tokenizer = tokenizer;
			_engine = engine;

			_sourceSegments = new List<Segment>();
			_targetSegments = new List<Segment>();
			_paragraphs = new HashSet<int>();
			_goToNextSegmentCommand = new RelayCommand<object>(o => GoToNextSegment(), o => CanGoToNextSegment());
			_goToPrevSegmentCommand = new RelayCommand<object>(o => GoToPrevSegment(), o => CanGoToPrevSegment());
			_approveSegmentCommand = new RelayCommand<object>(o => ApproveSegment(), o => CanApproveSegment());
			_applyAllSuggestionsCommand = new RelayCommand<object>(o => ApplyAllSuggestions(), o => CanApplyAllSuggestions());
			_selectSourceSegmentCommand = new RelayCommand<int>(SelectSourceSegment);
			_selectTargetSegmentCommand = new RelayCommand<int>(SelectTargetSegment);
			_suggestions = new BulkObservableList<SuggestionViewModel>();
			Suggestions = new ReadOnlyObservableList<SuggestionViewModel>(_suggestions);
			_sourceSegmentWords = new List<string>();
			_unapprovedTargetSegmentRanges = new BulkObservableList<Range<int>>();
			UnapprovedTargetSegmentRanges = new ReadOnlyObservableList<Range<int>>(_unapprovedTargetSegmentRanges);
			_alignedSourceWords = new BulkObservableList<AlignedWordViewModel>();
			AlignedSourceWords = new ReadOnlyObservableList<AlignedWordViewModel>(_alignedSourceWords);

			LoadSourceTextFile();
			SourceText = GenerateText(_sourceSegments);
			LoadTargetTextFile();
			while (_targetSegments.Count < _sourceSegments.Count)
				_targetSegments.Add(new Segment());
			TargetText = GenerateText(_targetSegments);
			UpdateUnapprovedTargetSegmentRanges();
		}

		public TextViewModel(ITokenizer<string, int> tokenizer)
			: this(tokenizer, null, null, null, null)
		{
		}

		public string Name { get; }

		internal double ConfidenceThreshold
		{
			get { return _confidenceThreshold; }
			set
			{
				if (Math.Abs(_confidenceThreshold - value) > double.Epsilon)
				{
					_confidenceThreshold = value;
					UpdateSuggestions();
					UpdateSourceSegmentSelection();
				}
			}
		}

		internal void SaveTargetText()
		{
			using (var writer = new StreamWriter(_targetFileName))
			{
				for (int i = 0; i < _targetSegments.Count; i++)
				{
					if (_paragraphs.Contains(i))
						writer.WriteLine();
					writer.WriteLine("{0}\t{1}", _targetSegments[i].IsApproved ? "1" : "0", _targetSegments[i].Text);
				}
			}
		}

		internal bool IsActive
		{
			get { return _isActive; }
			set
			{
				if (_isActive != value)
				{
					_isActive = value;
					if (_isActive)
					{
						if (_sourceSegments.Count > 0)
						{
							int unapprovedSegment = _targetSegments.FindIndex(s => !s.IsApproved);
							MoveSegment(unapprovedSegment == -1 ? 0 : unapprovedSegment);
						}
					}
					else
					{
						ResetSegment();
					}
				}
			}
		}

		internal IList<Segment> SourceSegments => _sourceSegments;
		internal IList<Segment> TargetSegments => _targetSegments;

		private void LoadSourceTextFile()
		{
			if (string.IsNullOrEmpty(_sourceFileName))
				return;

			foreach (string line in File.ReadAllLines(_sourceFileName))
			{
				if (line.Length > 0)
					_sourceSegments.Add(new Segment {Text = line});
				else
					_paragraphs.Add(_sourceSegments.Count);
			}
		}

		private void LoadTargetTextFile()
		{
			if (string.IsNullOrEmpty(_targetFileName))
				return;

			bool prevLinePara = false;
			foreach (string line in File.ReadAllLines(_targetFileName))
			{
				if (_paragraphs.Contains(_targetSegments.Count) && !prevLinePara)
				{
					prevLinePara = true;
				}
				else
				{
					int tabIndex = line.IndexOf("\t", StringComparison.Ordinal);
					bool isApproved = line.Substring(0, tabIndex) == "1";
					string text = line.Substring(tabIndex + 1);
					_targetSegments.Add(new Segment {Text = text, IsApproved = isApproved});
					_sourceSegments[_targetSegments.Count - 1].IsApproved = isApproved;
					prevLinePara = false;
				}
			}
		}

		private string GenerateText(List<Segment> segments)
		{
			bool addSpace = false;
			var sb = new StringBuilder();
			for (int i = 0; i < segments.Count; i++)
			{
				if (_paragraphs.Contains(i))
				{
					sb.AppendLine();
					sb.AppendLine();
					addSpace = false;
				}

				if (addSpace)
					sb.Append(" ");
				segments[i].StartIndex = sb.Length;
				sb.Append(segments[i].Text);
				addSpace = true;
			}
			return sb.ToString();
		}

		public ICommand GoToNextSegmentCommand => _goToNextSegmentCommand;

		private bool CanGoToNextSegment()
		{
			return _sourceSegments.Count > 0 && _currentSegment < _sourceSegments.Count - 1;
		}

		private void GoToNextSegment()
		{
			MoveSegment(_currentSegment + 1);
		}

		public ICommand GoToPrevSegmentCommand => _goToPrevSegmentCommand;

		private bool CanGoToPrevSegment()
		{
			return _sourceSegments.Count > 0 && _currentSegment > 0;
		}

		private void GoToPrevSegment()
		{
			MoveSegment(_currentSegment - 1);
		}

		public ICommand ApproveSegmentCommand => _approveSegmentCommand;

		private bool CanApproveSegment()
		{
			return _currentSegment != -1 && !_targetSegments[_currentSegment].IsApproved;
		}

		private void ApproveSegment()
		{
			_curSession.Approve();
			UpdateTargetText();
			_sourceSegments[_currentSegment].IsApproved = true;
			_targetSegments[_currentSegment].IsApproved = true;
			UpdateUnapprovedTargetSegmentRanges();
			UpdateSuggestions();
			_approveSegmentCommand.UpdateCanExecute();
			IsChanged = true;
		}

		public ICommand SelectSourceSegmentCommand => _selectSourceSegmentCommand;

		private void SelectSourceSegment(int index)
		{
			int segmentIndex = _sourceSegments.IndexOf(s => s.StartIndex <= index && s.StartIndex + s.Text.Length > index);
			if (segmentIndex != -1)
			{
				MoveSegment(segmentIndex);
				UpdateUnapprovedTargetSegmentRanges();
			}
		}

		public ICommand SelectTargetSegmentCommand => _selectTargetSegmentCommand;

		private void SelectTargetSegment(int index)
		{
			int segmentIndex = _targetSegments.IndexOf(s => s.StartIndex <= index && s.StartIndex + s.Text.Length > index);
			if (segmentIndex != -1)
				MoveSegment(segmentIndex);
		}

		private void MoveSegment(int segmentIndex)
		{
			EndSegmentTranslation();
			_currentSegment = segmentIndex;
			Segment sourceSegment = _sourceSegments[_currentSegment];
			Segment targetSegment = _targetSegments[_currentSegment];
			SourceSegment = sourceSegment.Text;
			TargetSegment = targetSegment.Text;

			CurrentSourceSegmentRange = new Range<int>(sourceSegment.StartIndex, sourceSegment.StartIndex + sourceSegment.Text.Length);
			CurrentTargetSegmentRange = new Range<int>(targetSegment.StartIndex, targetSegment.StartIndex + targetSegment.Text.Length);
			_goToNextSegmentCommand.UpdateCanExecute();
			_goToPrevSegmentCommand.UpdateCanExecute();
			_approveSegmentCommand.UpdateCanExecute();
			StartSegmentTranslation();
		}

		private void ResetSegment()
		{
			EndSegmentTranslation();
			_currentSegment = -1;
			SourceSegment = "";
			TargetSegment = "";
			_suggestions.Clear();
			_approveSegmentCommand.UpdateCanExecute();
			_applyAllSuggestionsCommand.UpdateCanExecute();
		}

		private void StartSegmentTranslation()
		{
			_sourceSegmentWords.AddRange(_tokenizer.TokenizeToStrings(_sourceSegments[_currentSegment].Text));
			_curSession = _engine.TranslateInteractively(_sourceSegments[_currentSegment].Text);
			_isTranslating = true;
			UpdatePrefix();
			UpdateSourceSegmentSelection();
		}

		private void UpdatePrefix()
		{
			if (!_isTranslating)
				return;

			_curSession.SetPrefix(_targetSegments[_currentSegment].Text, TargetSegment.Length == 0 || TargetSegment.EndsWith(" "));
			UpdateSuggestions();
		}

		private void UpdateSuggestions()
		{
			if (_currentSegment == -1)
				return;

			if (!_targetSegments[_currentSegment].IsApproved)
			{
				_suggestions.ReplaceAll(_curSession.GetSuggestedWordIndices(_confidenceThreshold)
					.Select(j => new SuggestionViewModel(this, _curSession.CurrentResult.RecaseTargetWord(_sourceSegmentWords, j))));
			}
			else
			{
				_suggestions.Clear();
			}
			_applyAllSuggestionsCommand.UpdateCanExecute();
		}

		private void EndSegmentTranslation()
		{
			_isTranslating = false;
			if (_curSession != null)
			{
				_curSession.Dispose();
				_curSession = null;
			}
			UpdateTargetText();
			_sourceSegmentWords.Clear();
			_suggestions.Clear();
			CurrentTargetSegmentIndex = 0;
			UpdateUnapprovedTargetSegmentRanges();
		}

		private void UpdateTargetText()
		{
			TargetText = GenerateText(_targetSegments);
		}

		public string SourceText
		{
			get { return _sourceText; }
			private set { Set(() => SourceText, ref _sourceText, value); }
		}

		public string TargetText
		{
			get { return _targetText; }
			private set { Set(() => TargetText, ref _targetText, value); }
		}

		public string SourceSegment
		{
			get { return _sourceSegment; }
			private set { Set(() => SourceSegment, ref _sourceSegment, value); }
		}

		public string TargetSegment
		{
			get { return _targetSegment; }
			set
			{
				if (Set(() => TargetSegment, ref _targetSegment, value))
				{
					if (_currentSegment != -1 && TargetSegment.Trim() != _targetSegments[_currentSegment].Text)
					{
						_targetSegments[_currentSegment].Text = TargetSegment.Trim();
						_sourceSegments[_currentSegment].IsApproved = false;
						_targetSegments[_currentSegment].IsApproved = false;
						IsChanged = true;
					}
					_approveSegmentCommand.UpdateCanExecute();
					UpdatePrefix();
				}
			}
		}

		public int CurrentTargetSegmentIndex
		{
			get { return _currentTargetSegmentIndex; }
			set
			{
				if (Set(() => CurrentTargetSegmentIndex, ref _currentTargetSegmentIndex, value))
					UpdateSourceSegmentSelection();
			}
		}

		private void UpdateSourceSegmentSelection()
		{
			if (!_isTranslating)
				return;

			var alignedSourceWords = new List<AlignedWordViewModel>();
			int targetWordIndex = _tokenizer.Tokenize(TargetSegment)
				.IndexOf(s => _currentTargetSegmentIndex >= s.Start && _currentTargetSegmentIndex <= s.End);
			TranslationResult result = _curSession.CurrentResult;
			if (targetWordIndex != -1)
			{
				double confidence = result.TargetWordConfidences[targetWordIndex];
				foreach (AlignedWordPair awi in result.GetTargetWordPairs(targetWordIndex)
					.Where(awi => confidence >= _confidenceThreshold || (awi.Sources & TranslationSources.Transfer) != 0))
				{
					WordTranslationLevel level;
					Span<int> span = _tokenizer.Tokenize(SourceSegment).ElementAt(awi.SourceIndex);
					if ((awi.Sources & TranslationSources.Transfer) == TranslationSources.Transfer)
						level = WordTranslationLevel.Transfer;
					else if (confidence >= 0.5f)
						level = WordTranslationLevel.HighConfidence;
					else
						level = WordTranslationLevel.LowConfidence;
					alignedSourceWords.Add(new AlignedWordViewModel(new Range<int>(span.Start, span.End - 1), level));
				}
			}

			_alignedSourceWords.ReplaceAll(alignedSourceWords);
		}

		public ReadOnlyObservableList<AlignedWordViewModel> AlignedSourceWords { get; }

		public Range<int>? CurrentSourceSegmentRange
		{
			get { return _currentSourceSegmentRange; }
			private set { Set(() => CurrentSourceSegmentRange, ref _currentSourceSegmentRange, value); }
		}

		public Range<int>? CurrentTargetSegmentRange
		{
			get { return _currentTargetSegmentRange; }
			private set { Set(() => CurrentTargetSegmentRange, ref _currentTargetSegmentRange, value); }
		}

		public ReadOnlyObservableList<SuggestionViewModel> Suggestions { get; }

		public ICommand ApplyAllSuggestionsCommand => _applyAllSuggestionsCommand;

		private bool CanApplyAllSuggestions()
		{
			return _suggestions.Count > 0;
		}

		private void ApplyAllSuggestions()
		{
			foreach (SuggestionViewModel suggestion in _suggestions.ToArray())
				suggestion.InsertSuggestion();
		}

		public ReadOnlyObservableList<Range<int>> UnapprovedTargetSegmentRanges { get; }

		private void UpdateUnapprovedTargetSegmentRanges()
		{
			_unapprovedTargetSegmentRanges.ReplaceAll(_targetSegments.Where(s => !s.IsApproved && !string.IsNullOrEmpty(s.Text))
				.Select(s => new Range<int>(s.StartIndex, s.StartIndex + s.Text.Length)));
		}

		public void AcceptChanges()
		{
			IsChanged = false;
		}

		public bool IsChanged
		{
			get { return _isChanged; }
			private set { Set(() => IsChanged, ref _isChanged, value); }
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
