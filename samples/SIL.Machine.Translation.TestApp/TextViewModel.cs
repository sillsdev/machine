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
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.TestApp
{
	public class TextViewModel : ViewModelBase, IChangeTracking
	{
		private readonly IRangeTokenizer<string, int, string> _tokenizer;
		private readonly string _metadataFileName;
		private readonly string _targetFileName;
		private readonly string _alignmentFileName;

		private string _sourceText;
		private string _targetText;
		private string _sourceSegment;
		private string _targetSegment;
		private int _currentTargetSegmentIndex;
		private readonly RelayCommand _goToNextSegmentCommand;
		private readonly RelayCommand _goToPrevSegmentCommand;
		private readonly RelayCommand _approveSegmentCommand; 
		private readonly RelayCommand _applyAllSuggestionsCommand; 
		private readonly List<Segment> _sourceSegments;
		private readonly List<Segment> _targetSegments;
		private readonly HashSet<int> _approvedSegments;
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
		private readonly ITranslationSuggester _suggester;
		private bool _isChanged;
		private bool _isActive;
		private bool _isTranslating;
		private HybridInteractiveTranslationSession _curSession;

		public TextViewModel(IRangeTokenizer<string, int, string> tokenizer, string name, string metadataFileName,
			string sourceFileName, string targetFileName, string alignmentFileName)
		{
			Name = name;
			_metadataFileName = metadataFileName;
			_targetFileName = targetFileName;
			_alignmentFileName = alignmentFileName;
			_tokenizer = tokenizer;

			_sourceSegments = new List<Segment>();
			_targetSegments = new List<Segment>();
			_approvedSegments = new HashSet<int>();
			_paragraphs = new HashSet<int>();
			_goToNextSegmentCommand = new RelayCommand(GoToNextSegment, CanGoToNextSegment);
			_goToPrevSegmentCommand = new RelayCommand(GoToPrevSegment, CanGoToPrevSegment);
			_approveSegmentCommand = new RelayCommand(ApproveSegment, CanApproveSegment);
			_applyAllSuggestionsCommand = new RelayCommand(ApplyAllSuggestions, CanApplyAllSuggestions);
			_selectSourceSegmentCommand = new RelayCommand<int>(SelectSourceSegment);
			_selectTargetSegmentCommand = new RelayCommand<int>(SelectTargetSegment);
			_suggestions = new BulkObservableList<SuggestionViewModel>();
			Suggestions = new ReadOnlyObservableList<SuggestionViewModel>(_suggestions);
			_sourceSegmentWords = new List<string>();
			_unapprovedTargetSegmentRanges = new BulkObservableList<Range<int>>();
			UnapprovedTargetSegmentRanges = new ReadOnlyObservableList<Range<int>>(
				_unapprovedTargetSegmentRanges);
			_alignedSourceWords = new BulkObservableList<AlignedWordViewModel>();
			AlignedSourceWords = new ReadOnlyObservableList<AlignedWordViewModel>(_alignedSourceWords);
			_suggester = new WordTranslationSuggester() { ConfidenceThreshold = 0.2 };

			LoadMetadataFile();
			LoadTextFile(sourceFileName, _sourceSegments);
			SourceText = GenerateText(_sourceSegments);
			LoadTextFile(_targetFileName, _targetSegments);
			TargetText = GenerateText(_targetSegments);
			UpdateUnapprovedTargetSegmentRanges();
		}

		public TextViewModel(IRangeTokenizer<string, int, string> tokenizer)
			: this(tokenizer, null, null, null, null, null)
		{
		}

		public string Name { get; }

		internal HybridTranslationEngine Engine { get; set; }

		internal double ConfidenceThreshold
		{
			get { return _suggester.ConfidenceThreshold; }
			set
			{
				if (_suggester.ConfidenceThreshold != value)
				{
					_suggester.ConfidenceThreshold = value;
					UpdateSuggestions();
					UpdateSourceSegmentSelection();
				}
			}
		}

		internal void Save()
		{
			if (!IsChanged)
				return;

			using (var metadataWriter = new StreamWriter(_metadataFileName))
			using (var textWriter = new StreamWriter(_targetFileName))
			{
				for (int i = 0; i < _targetSegments.Count; i++)
				{
					metadataWriter.WriteLine("{0}\t{1}", _paragraphs.Contains(i) || i == 0 ? "1" : "0",
						_approvedSegments.Contains(i) ? "1" : "0");
					textWriter.WriteLine(_targetSegments[i].Text);
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
							int unapprovedSegment = Enumerable.Range(0, _sourceSegments.Count)
								.FirstOrDefault(i => !_approvedSegments.Contains(i));
							MoveSegment(unapprovedSegment);
						}
					}
					else
					{
						ResetSegment();
					}
				}
			}
		}

		internal bool IsApproved(TextSegmentRef segmentRef)
		{
			return _approvedSegments.Contains(int.Parse(segmentRef.Keys[1]) - 1);
		}

		private void LoadMetadataFile()
		{
			if (string.IsNullOrEmpty(_metadataFileName))
				return;

			foreach (string line in File.ReadAllLines(_metadataFileName))
			{
				if (line == string.Empty)
					continue;

				int tabIndex = line.IndexOf('\t');
				if (line.Substring(0, tabIndex) == "1" && _sourceSegments.Count != 0)
					_paragraphs.Add(_sourceSegments.Count);
				if (line.Substring(tabIndex + 1) == "1")
					_approvedSegments.Add(_sourceSegments.Count);
				_sourceSegments.Add(new Segment());
				_targetSegments.Add(new Segment());
			}
		}

		private void LoadTextFile(string fileName, List<Segment> segments)
		{
			if (string.IsNullOrEmpty(fileName))
				return;

			int i = 0;
			foreach (string line in File.ReadAllLines(fileName))
			{
				if (i >= segments.Count)
					break;

				segments[i].Text = line;
				i++;
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
			return _currentSegment != -1 && !_approvedSegments.Contains(_currentSegment);
		}

		private void ApproveSegment()
		{
			_curSession.Approve(false);
			UpdateTargetText();
			_approvedSegments.Add(_currentSegment);
			UpdateUnapprovedTargetSegmentRanges();
			UpdateSuggestions();
			_approveSegmentCommand.UpdateCanExecute();
			IsChanged = true;
		}

		private (int, int)[] GetAlignedWords(WordAlignmentMatrix matrix)
		{
			return Enumerable.Range(0, matrix.RowCount)
				.SelectMany(i => Enumerable.Range(0, matrix.ColumnCount), (s, t) => (SourceIndex: s, TargetIndex: t))
				.Where(t => matrix[t.SourceIndex, t.TargetIndex]).ToArray();
		}

		public ICommand SelectSourceSegmentCommand => _selectSourceSegmentCommand;

		private void SelectSourceSegment(int index)
		{
			int segmentIndex = _sourceSegments
				.IndexOf(s => s.StartIndex <= index && s.StartIndex + s.Text.Length > index);
			if (segmentIndex != -1)
			{
				MoveSegment(segmentIndex);
				UpdateUnapprovedTargetSegmentRanges();
			}
		}

		public ICommand SelectTargetSegmentCommand => _selectTargetSegmentCommand;

		private void SelectTargetSegment(int index)
		{
			int segmentIndex = _targetSegments
				.IndexOf(s => s.StartIndex <= index && s.StartIndex + s.Text.Length > index);
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

			CurrentSourceSegmentRange = new Range<int>(sourceSegment.StartIndex,
				sourceSegment.StartIndex + sourceSegment.Text.Length);
			CurrentTargetSegmentRange = new Range<int>(targetSegment.StartIndex,
				targetSegment.StartIndex + targetSegment.Text.Length);
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
			_sourceSegmentWords.AddRange(_tokenizer.Tokenize(_sourceSegments[_currentSegment].Text));
			_curSession = Engine.TranslateInteractively(1, _sourceSegmentWords.Process(StringProcessors.Lowercase));
			_isTranslating = true;
			UpdatePrefix();
			UpdateSourceSegmentSelection();
		}

		private void UpdatePrefix()
		{
			if (!_isTranslating)
				return;

			IReadOnlyList<string> prefix = _tokenizer.Tokenize(_targetSegments[_currentSegment].Text)
				.Process(StringProcessors.Lowercase);
			_curSession.SetPrefix(prefix, TargetSegment.Length == 0 || TargetSegment.EndsWith(" "));
			UpdateSuggestions();
		}

		private void UpdateSuggestions()
		{
			if (_currentSegment == -1)
				return;

			if (!_approvedSegments.Contains(_currentSegment))
			{
				_suggestions.ReplaceAll(_suggester.GetSuggestions(_curSession).First().TargetWordIndices.Select(j =>
					new SuggestionViewModel(this,
						_curSession.CurrentResults[0].RecaseTargetWord(_sourceSegmentWords, j))));
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
			get => _sourceText;
			private set => Set(nameof(SourceText), ref _sourceText, value);
		}

		public string TargetText
		{
			get => _targetText;
			private set => Set(nameof(TargetText), ref _targetText, value);
		}

		public string SourceSegment
		{
			get => _sourceSegment;
			private set => Set(nameof(SourceSegment), ref _sourceSegment, value);
		}

		public string TargetSegment
		{
			get => _targetSegment;
			set
			{
				if (Set(nameof(TargetSegment), ref _targetSegment, value))
				{
					if (_currentSegment != -1 && TargetSegment.Trim() != _targetSegments[_currentSegment].Text)
					{
						_targetSegments[_currentSegment].Text = TargetSegment.Trim();
						_approvedSegments.Remove(_currentSegment);
						IsChanged = true;
					}
					_approveSegmentCommand.UpdateCanExecute();
					UpdatePrefix();
				}
			}
		}

		public int CurrentTargetSegmentIndex
		{
			get => _currentTargetSegmentIndex;
			set
			{
				if (Set(nameof(CurrentTargetSegmentIndex), ref _currentTargetSegmentIndex, value))
					UpdateSourceSegmentSelection();
			}
		}

		private void UpdateSourceSegmentSelection()
		{
			if (!_isTranslating)
				return;

			var alignedSourceWords = new List<AlignedWordViewModel>();
			int targetWordIndex = _tokenizer.TokenizeAsRanges(TargetSegment)
				.IndexOf(s => _currentTargetSegmentIndex >= s.Start && _currentTargetSegmentIndex <= s.End);
			TranslationResult result = _curSession.CurrentResults[0];
			if (targetWordIndex != -1)
			{
				double confidence = result.WordConfidences[targetWordIndex];
				TranslationSources sources = result.WordSources[targetWordIndex];
				if (confidence >= _suggester.ConfidenceThreshold || (sources & TranslationSources.Transfer) != 0)
				{
					foreach (int sourceIndex in result.Alignment.GetColumnAlignedIndices(targetWordIndex))
					{
						WordTranslationLevel level;
						Annotations.Range<int> range = _tokenizer.TokenizeAsRanges(SourceSegment)
							.ElementAt(sourceIndex);
						if ((sources & TranslationSources.Transfer) != 0)
							level = WordTranslationLevel.Transfer;
						else if (confidence >= 0.5f)
							level = WordTranslationLevel.HighConfidence;
						else
							level = WordTranslationLevel.LowConfidence;
						alignedSourceWords.Add(
							new AlignedWordViewModel(new Range<int>(range.Start, range.End - 1), level));
					}
				}
			}

			_alignedSourceWords.ReplaceAll(alignedSourceWords);
		}

		public ReadOnlyObservableList<AlignedWordViewModel> AlignedSourceWords { get; }

		public Range<int>? CurrentSourceSegmentRange
		{
			get => _currentSourceSegmentRange;
			private set => Set(nameof(CurrentSourceSegmentRange), ref _currentSourceSegmentRange, value);
		}

		public Range<int>? CurrentTargetSegmentRange
		{
			get => _currentTargetSegmentRange;
			private set => Set(nameof(CurrentTargetSegmentRange), ref _currentTargetSegmentRange, value);
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
			_unapprovedTargetSegmentRanges.ReplaceAll(_targetSegments
				.Where((s, i) => !_approvedSegments.Contains(i) && !string.IsNullOrEmpty(s.Text))
				.Select(s => new Range<int>(s.StartIndex, s.StartIndex + s.Text.Length)));
		}

		public void AcceptChanges()
		{
			IsChanged = false;
		}

		public bool IsChanged
		{
			get => _isChanged;
			private set => Set(nameof(IsChanged), ref _isChanged, value);
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
