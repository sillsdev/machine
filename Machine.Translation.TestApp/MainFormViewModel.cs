using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Eto.Forms;
using GalaSoft.MvvmLight;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.HermitCrab;
using SIL.Machine.Translation.HermitCrab;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.TestApp
{
	public enum TranslationLevel
	{
		Unknown,
		Transfer,
		HighConfidence,
		LowConfidence
	}

	public class MainFormViewModel : ViewModelBase
	{
		private static readonly Regex TokenizeRegex = new Regex(@"\w+([.,]\w+)*|\S+");

		private string _sourceText;
		private string _targetText;
		private string _sourceSegment;
		private string _targetSegment;
		private int _currentTargetSegmentIndex;
		private readonly RelayCommand<object> _openProjectCommand;
		private readonly RelayCommand<object> _saveProjectCommand; 
		private readonly RelayCommand<object> _approveSegmentCommand; 
		private readonly RelayCommand<object> _closeCommand;
		private readonly RelayCommand<object> _applyAllSuggestionsCommand; 
		private readonly List<Segment> _sourceSegments;
		private readonly List<Segment> _targetSegments;
		private int _currentSegment = -1;
		private TranslationEngine _engine;
		private SegmentTranslator _translator;
		private readonly ShapeSpanFactory _spanFactory;
		private readonly TraceManager _hcTraceManager;
		private readonly HashSet<int> _paragraphs;
		private readonly BulkObservableList<SuggestionViewModel> _suggestions; 
		private readonly ReadOnlyObservableList<SuggestionViewModel> _readOnlySuggestions;
		private string _sourceTextPath;
		private string _targetTextPath;
		private readonly List<string> _sourceSegmentWords;
		private Range<int>? _currentSourceWordRange;
		private Range<int>? _currentSourceSegmentRange;
		private Range<int>? _currentTargetSegmentRange; 
		private int _confidenceThreshold;
		private readonly RelayCommand<int> _selectSourceSegmentCommand;
		private readonly RelayCommand<int> _selectTargetSegmentCommand;
		private TranslationLevel _currentSourceWordLevel;
		private bool _updated;
		private readonly BulkObservableList<Range<int>> _unapprovedTargetSegmentRanges;
		private readonly ReadOnlyObservableList<Range<int>> _readonlyUnapprovedTargetSegmentRanges;

		public MainFormViewModel()
		{
			_sourceSegments = new List<Segment>();
			_targetSegments = new List<Segment>();
			_paragraphs = new HashSet<int>();
			_openProjectCommand = new RelayCommand<object>(o => OpenProject());
			_saveProjectCommand = new RelayCommand<object>(o => SaveProject(), o => IsUpdated);
			_approveSegmentCommand = new RelayCommand<object>(o => ApproveSegment(), o => CanApproveSegment());
			_closeCommand = new RelayCommand<object>(o => Close(), o => CanClose());
			_applyAllSuggestionsCommand = new RelayCommand<object>(o => ApplyAllSuggestions(), o => CanApplyAllSuggestions());
			_selectSourceSegmentCommand = new RelayCommand<int>(SelectSourceSegment);
			_selectTargetSegmentCommand = new RelayCommand<int>(SelectTargetSegment);
			_spanFactory = new ShapeSpanFactory();
			_hcTraceManager = new TraceManager();
			_suggestions = new BulkObservableList<SuggestionViewModel>();
			_readOnlySuggestions = new ReadOnlyObservableList<SuggestionViewModel>(_suggestions);
			_sourceSegmentWords = new List<string>();
			_confidenceThreshold = 20;
			_unapprovedTargetSegmentRanges = new BulkObservableList<Range<int>>();
			_readonlyUnapprovedTargetSegmentRanges = new ReadOnlyObservableList<Range<int>>(_unapprovedTargetSegmentRanges);
		}

		private bool IsUpdated
		{
			get { return _updated; }
			set
			{
				if (_updated != value)
				{
					_updated = value;
					_saveProjectCommand.UpdateCanExecute();
				}
			}
		}

		public ICommand OpenProjectCommand
		{
			get { return _openProjectCommand; }
		}

		private void OpenProject()
		{
			using (var dialog = new OpenFileDialog {Title = "Open Project", CheckFileExists = true, Filters = {new FileDialogFilter("Project files", ".catx")}} )
			{
				if (dialog.ShowDialog(null) == DialogResult.Ok)
				{
					CloseProject();
					if (!LoadProject(dialog.FileName))
						MessageBox.Show("There was an error loading the project configuration file.", MessageBoxButtons.OK, MessageBoxType.Error);
				}
			}
		}

		private bool LoadProject(string fileName)
		{
			XElement projectElem;
			try
			{
				projectElem = XElement.Load(fileName);
			}
			catch (Exception)
			{
				return false;
			}

			XElement engineElem = projectElem.Element("TranslationEngine");
			if (engineElem == null)
				return false;

			var smtConfig = (string) engineElem.Element("SmtConfig");
			if (smtConfig == null)
				return false;

			var hcSrcConfig = (string) engineElem.Element("SourceAnalyzerConfig");
			var hcTrgConfig = (string) engineElem.Element("TargetGeneratorConfig");

			XElement textElem = projectElem.Elements("Texts").Elements("Text").FirstOrDefault();
			if (textElem == null)
				return false;

			var srcTextFile = (string) textElem.Element("SourceFile");
			if (srcTextFile == null)
				return false;

			var trgTextFile = (string) textElem.Element("TargetFile");
			if (trgTextFile == null)
				return false;

			string configDir = Path.GetDirectoryName(fileName) ?? "";

			_sourceTextPath = Path.Combine(configDir, srcTextFile);
			LoadSourceTextFile();
			SourceText = GenerateText(_sourceSegments);
			_targetTextPath = Path.Combine(configDir, trgTextFile);
			LoadTargetTextFile();
			while (_targetSegments.Count < _sourceSegments.Count)
				_targetSegments.Add(new Segment());
			TargetText = GenerateText(_targetSegments);
			UpdateUnapprovedTargetSegmentRanges();

			TransferEngine transferEngine = null;
			if (hcSrcConfig != null && hcTrgConfig != null)
			{
				Language srcLang = XmlLoader.Load(Path.Combine(configDir, hcSrcConfig));
				var srcMorpher = new Morpher(_spanFactory, _hcTraceManager, srcLang);
				var srcAnalyzer = new HermitCrabSourceAnalyzer(GetMorphemeId, GetCategory, srcMorpher);

				Language trgLang = XmlLoader.Load(Path.Combine(configDir, hcTrgConfig));
				var trgMorpher = new Morpher(_spanFactory, _hcTraceManager, trgLang);
				var trgGenerator = new HermitCrabTargetGenerator(GetMorphemeId, GetCategory, trgMorpher);

				transferEngine = new TransferEngine(srcAnalyzer, new SimpleTransferer(new GlossMorphemeMapper(trgGenerator)), trgGenerator);
			}
			var smtEngine = new ThotSmtEngine(Path.Combine(configDir, smtConfig));
			_engine = new TranslationEngine(smtEngine, transferEngine);

			if (_sourceSegments.Count > 0)
			{
				int unapprovedSegment = _targetSegments.FindIndex(s => !s.IsApproved);
				MoveSegment(unapprovedSegment == -1 ? 0 : unapprovedSegment);
			}
			else
			{
				ResetSegment();
			}
			IsUpdated = false;
			_saveProjectCommand.UpdateCanExecute();
			return true;
		}

		public ICommand SaveProjectCommand
		{
			get { return _saveProjectCommand; }
		}

		private void SaveProject()
		{
			if (_engine != null)
				_engine.Save();
			if (!string.IsNullOrEmpty(_targetTextPath))
				SaveTargetText();
			IsUpdated = false;
		}

		private void CloseProject()
		{
			ResetSegment();
			if (_engine != null)
			{
				_engine.Dispose();
				_engine = null;
			}

			_sourceSegments.Clear();
			_targetSegments.Clear();
			SourceText = "";
			TargetText = "";
			_unapprovedTargetSegmentRanges.Clear();
		}

		private void SaveTargetText()
		{
			using (var writer = new StreamWriter(_targetTextPath))
			{
				for (int i = 0; i < _targetSegments.Count; i++)
				{
					if (_paragraphs.Contains(i))
						writer.WriteLine();
					writer.WriteLine("{0}\t{1}", _targetSegments[i].IsApproved ? "1" : "0", _targetSegments[i].Text);
				}
			}
		}

		private void LoadSourceTextFile()
		{
			foreach (string line in File.ReadAllLines(_sourceTextPath))
			{
				if (line.Length > 0)
					_sourceSegments.Add(new Segment {Text = line});
				else
					_paragraphs.Add(_sourceSegments.Count);
			}
		}

		private void LoadTargetTextFile()
		{
			bool prevLinePara = false;
			foreach (string line in File.ReadAllLines(_targetTextPath))
			{
				if (_paragraphs.Contains(_targetSegments.Count) && !prevLinePara)
				{
					prevLinePara = true;
				}
				else
				{
					int tabIndex = line.IndexOf("\t", StringComparison.Ordinal);
					string approved = line.Substring(0, tabIndex);
					string text = line.Substring(tabIndex + 1);
					_targetSegments.Add(new Segment {Text = text, IsApproved = approved == "1"});
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

		private static string GetMorphemeId(Morpheme morpheme)
		{
			return morpheme.Gloss;
		}

		private static string GetCategory(FeatureStruct fs)
		{
			SymbolicFeatureValue value;
			if (fs.TryGetValue("pos", out value))
				return value.Values.First().ID;
			return null;
		}

		private bool CanApproveSegment()
		{
			return _currentSegment != -1 && !_targetSegments[_currentSegment].IsApproved;
		}

		private void ApproveSegment()
		{
			_translator.Approve();
			Segment targetSegment = _targetSegments[_currentSegment];
			if (_currentSegment == _sourceSegments.Count - 1)
				UpdateTargetText();
			else
				MoveSegment(_currentSegment + 1);
			targetSegment.IsApproved = true;
			UpdateUnapprovedTargetSegmentRanges();
			IsUpdated = true;
		}

		public ICommand ApproveSegmentCommand
		{
			get { return _approveSegmentCommand; }
		}

		public ICommand SelectSourceSegmentCommand
		{
			get { return _selectSourceSegmentCommand; }
		}

		private void SelectSourceSegment(int index)
		{
			int segmentIndex = _sourceSegments.IndexOf(s => s.StartIndex <= index && s.StartIndex + s.Text.Length > index);
			if (segmentIndex != -1)
			{
				MoveSegment(segmentIndex);
				UpdateUnapprovedTargetSegmentRanges();
			}
		}

		public ICommand SelectTargetSegmentCommand
		{
			get { return _selectTargetSegmentCommand; }
		}

		private void SelectTargetSegment(int index)
		{
			int segmentIndex = _targetSegments.IndexOf(s => s.StartIndex <= index && s.StartIndex + s.Text.Length > index);
			if (segmentIndex != -1)
			{
				MoveSegment(segmentIndex);
				UpdateUnapprovedTargetSegmentRanges();
			}
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
			MatchCollection matches = TokenizeRegex.Matches(SourceSegment);
			_sourceSegmentWords.AddRange(matches.Cast<Match>().Select(m => m.Value));
			_translator = _engine.StartSegmentTranslation(_sourceSegmentWords.Select(w => w.ToLowerInvariant()));
			UpdatePrefix();
			UpdateSourceSegmentSelection();
		}

		private void UpdateSuggestions()
		{
			var suggestions = new List<SuggestionViewModel>();
			int lookaheadCount = Math.Max(0, _translator.Translation.Count - _translator.SourceSegment.Count) + 1;
			int i = _translator.Prefix.Count;
			if (_translator.IsLastWordPartial)
			{
				lookaheadCount--;
				i--;
			}
			bool inPhrase = false;
			while (i < _translator.Translation.Count && (i < _translator.Prefix.Count + lookaheadCount || inPhrase))
			{
				string word = _translator.Translation[i];
				if (IsWordSignificant(i))
				{
					if (word.All(char.IsPunctuation))
					{
						if (word.IsOneOf(".", ",") && suggestions.Count > 0)
						{
							string prevWord = suggestions[suggestions.Count - 1].Text;
							suggestions[suggestions.Count - 1] = new SuggestionViewModel(this, prevWord + word);
						}
						inPhrase = false;
					}
					else
					{
						if (IsCapitalCase(_sourceSegmentWords[_translator.GetSourceWordIndex(i)]))
							word = ToCapitalCase(word);
						if (suggestions.Count == 0 || suggestions[suggestions.Count - 1].Text != word)
							suggestions.Add(new SuggestionViewModel(this, word));
						inPhrase = true;
					}
				}
				else
				{
					inPhrase = false;
				}
				i++;
			}

			_suggestions.ReplaceAll(suggestions);
			_applyAllSuggestionsCommand.UpdateCanExecute();
		}

		private bool IsWordSignificant(int index)
		{
			return _translator.IsWordTransferred(index) || _translator.GetWordConfidence(index) >= (_confidenceThreshold / 100.0f);
		}

		private static bool IsCapitalCase(string word)
		{
			return word.Length > 0 && char.IsUpper(word, 0) && Enumerable.Range(1, word.Length - 1).All(i => char.IsLower(word, i));
		}

		private static string ToCapitalCase(string word)
		{
			if (word.Length == 0)
				return word;

			var sb = new StringBuilder();
			sb.Append(word.Substring(0, 1).ToUpperInvariant());
			if (word.Length > 1)
				sb.Append(word.Substring(1, word.Length - 1).ToLowerInvariant());
			return sb.ToString();
		}

		private void EndSegmentTranslation()
		{
			UpdateTargetText();
			_translator = null;
			_sourceSegmentWords.Clear();
			CurrentTargetSegmentIndex = 0;
		}

		private void UpdateTargetText()
		{
			TargetText = GenerateText(_targetSegments);
		}

		public ICommand CloseCommand
		{
			get { return _closeCommand; }
		}

		private bool CanClose()
		{
			if (IsUpdated)
			{
				DialogResult result = MessageBox.Show("Do you wish to save the project before exiting?", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
				switch (result)
				{
					case DialogResult.Yes:
						SaveProject();
						return true;

					case DialogResult.No:
						return true;

					case DialogResult.Cancel:
						return false;
				}
			}

			return true;
		}

		private void Close()
		{
			CloseProject();
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
					UpdatePrefix();
					if (_currentSegment != -1 && TargetSegment.Trim() != _targetSegments[_currentSegment].Text)
					{
						_targetSegments[_currentSegment].Text = TargetSegment.Trim();
						_targetSegments[_currentSegment].IsApproved = false;
						IsUpdated = true;
					}
					_approveSegmentCommand.UpdateCanExecute();
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
			if (_translator == null)
				return;

			int targetWordIndex = TokenizeRegex.Matches(TargetSegment).Cast<Match>()
				.IndexOf(m => _currentTargetSegmentIndex >= m.Index && _currentTargetSegmentIndex <= m.Index + m.Length);
			if (targetWordIndex != -1)
			{
				if (IsWordSignificant(targetWordIndex))
				{
					int sourceWordIndex = _translator.GetSourceWordIndex(targetWordIndex);
					Match match = TokenizeRegex.Matches(SourceSegment)[sourceWordIndex];
					if (_translator.IsWordTransferred(targetWordIndex))
						CurrentSourceWordLevel = TranslationLevel.Transfer;
					else if (_translator.GetWordConfidence(targetWordIndex) >= 0.5f)
						CurrentSourceWordLevel = TranslationLevel.HighConfidence;
					else
						CurrentSourceWordLevel = TranslationLevel.LowConfidence;
					CurrentSourceWordRange = new Range<int>(match.Index, match.Index + match.Length - 1);
				}
				else
				{
					CurrentSourceWordLevel = TranslationLevel.Unknown;
					CurrentSourceWordRange = null;
				}
			}
		}

		public Range<int>? CurrentSourceSegmentRange
		{
			get { return _currentSourceSegmentRange; }
			private set { Set(() => CurrentSourceSegmentRange, ref _currentSourceSegmentRange, value); }
		}

		public Range<int>? CurrentSourceWordRange
		{
			get { return _currentSourceWordRange; }
			private set { Set(() => CurrentSourceWordRange, ref _currentSourceWordRange, value); }
		}

		public Range<int>? CurrentTargetSegmentRange
		{
			get { return _currentTargetSegmentRange; }
			private set { Set(() => CurrentTargetSegmentRange, ref _currentTargetSegmentRange, value); }
		}

		public TranslationLevel CurrentSourceWordLevel
		{
			get { return _currentSourceWordLevel; }
			private set { Set(() => CurrentSourceWordLevel, ref _currentSourceWordLevel, value); }
		}

		public int ConfidenceThreshold
		{
			get { return _confidenceThreshold; }
			set
			{
				if (Set(() => ConfidenceThreshold, ref _confidenceThreshold, value))
				{
					UpdateSuggestions();
					UpdateSourceSegmentSelection();
				}
			}
		}

		private void UpdatePrefix()
		{
			if (_translator == null)
				return;

			_translator.SetPrefix(TokenizeRegex.Matches(TargetSegment).Cast<Match>().Select(m => m.Value.ToLowerInvariant()),
				TargetSegment.Length > 0 && !TargetSegment.EndsWith(" "));
			UpdateSuggestions();
		}

		public ReadOnlyObservableList<SuggestionViewModel> Suggestions
		{
			get { return _readOnlySuggestions; }
		}

		public ICommand ApplyAllSuggestionsCommand
		{
			get { return _applyAllSuggestionsCommand; }
		}

		private bool CanApplyAllSuggestions()
		{
			return _suggestions.Count > 0;
		}

		private void ApplyAllSuggestions()
		{
			foreach (SuggestionViewModel suggestion in _suggestions.ToArray())
				suggestion.InsertSuggestion();
		}

		public ReadOnlyObservableList<Range<int>> UnapprovedTargetSegmentRanges
		{
			get { return _readonlyUnapprovedTargetSegmentRanges; }
		}

		private void UpdateUnapprovedTargetSegmentRanges()
		{
			_unapprovedTargetSegmentRanges.ReplaceAll(_targetSegments.Where(s => !s.IsApproved && !string.IsNullOrEmpty(s.Text))
				.Select(s => new Range<int>(s.StartIndex, s.StartIndex + s.Text.Length)));
		}
	}
}
