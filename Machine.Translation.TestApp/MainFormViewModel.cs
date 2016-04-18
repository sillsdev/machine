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
	public class MainFormViewModel : ViewModelBase
	{
		private static readonly Regex TokenizeRegex = new Regex(@"\w+([.,]\w+)*|\S+");

		private string _sourceText;
		private string _targetText;
		private string _sourceSegment;
		private string _targetSegment;
		private int _currentTargetSegmentIndex;
		private readonly RelayCommand<object> _openProjectCommand;
		private readonly RelayCommand<object> _goToNextSegmentCommand;
		private readonly RelayCommand<object> _goToPrevSegmentCommand;
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
		private Range<int> _sourceSegmentSelection; 

		public MainFormViewModel()
		{
			_sourceSegments = new List<Segment>();
			_targetSegments = new List<Segment>();
			_paragraphs = new HashSet<int>();
			_openProjectCommand = new RelayCommand<object>(o => OpenProject());
			_goToNextSegmentCommand = new RelayCommand<object>(o => GoToNextSegment(), o => CanGoToNextSegment());
			_goToPrevSegmentCommand = new RelayCommand<object>(o => GoToPrevSegment(), o => CanGoToPrevSegment());
			_closeCommand = new RelayCommand<object>(o => Close());
			_applyAllSuggestionsCommand = new RelayCommand<object>(o => ApplyAllSuggestions());
			_spanFactory = new ShapeSpanFactory();
			_hcTraceManager = new TraceManager();
			_suggestions = new BulkObservableList<SuggestionViewModel>();
			_readOnlySuggestions = new ReadOnlyObservableList<SuggestionViewModel>(_suggestions);
			_sourceSegmentWords = new List<string>();
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
			if (hcSrcConfig == null)
				return false;

			var hcTrgConfig = (string) engineElem.Element("TargetGeneratorConfig");
			if (hcTrgConfig == null)
				return false;

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

			Language srcLang = XmlLoader.Load(Path.Combine(configDir, hcSrcConfig));
			var srcMorpher = new Morpher(_spanFactory, _hcTraceManager, srcLang);
			var srcAnalyzer = new HermitCrabSourceAnalyzer(GetMorphemeId, GetCategory, srcMorpher);

			Language trgLang = XmlLoader.Load(Path.Combine(configDir, hcTrgConfig));
			var trgMorpher = new Morpher(_spanFactory, _hcTraceManager, trgLang);
			var trgGenerator = new HermitCrabTargetGenerator(GetMorphemeId, GetCategory, trgMorpher);

			var transferEngine = new TransferEngine(srcAnalyzer, new SimpleTransferer(new GlossMorphemeMapper(trgGenerator)), trgGenerator);
			var smtEngine = new ThotSmtEngine(Path.Combine(configDir, smtConfig));
			_engine = new TranslationEngine(smtEngine, transferEngine);

			if (_sourceSegments.Count > 0)
			{
				_currentSegment = _targetSegments.FindIndex(s => string.IsNullOrEmpty(s.Text)) - 1;
				GoToNextSegment();
			}
			else
			{
				ResetSegment();
			}

			return true;
		}

		private void CloseProject()
		{
			ResetSegment();
			if (_engine != null)
			{
				_engine.Save();
				_engine.Dispose();
				_engine = null;
			}

			if (!string.IsNullOrEmpty(_targetTextPath))
				SaveTargetText();

			_sourceSegments.Clear();
			_targetSegments.Clear();
			SourceText = "";
			TargetText = "";
		}

		private void SaveTargetText()
		{
			using (var writer = new StreamWriter(_targetTextPath))
			{
				for (int i = 0; i < _targetSegments.Count; i++)
				{
					if (_paragraphs.Contains(i))
						writer.WriteLine();
					writer.WriteLine(_targetSegments[i].Text);
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
					_targetSegments.Add(new Segment {Text = line});
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

		public ICommand GoToNextSegmentCommand
		{
			get { return _goToNextSegmentCommand; }
		}

		private bool CanGoToNextSegment()
		{
			return _sourceSegments.Count > 0 && _currentSegment < _sourceSegments.Count - 1;
		}

		private void GoToNextSegment()
		{
			EndSegmentTranslation();
			_currentSegment++;
			SourceSegment = _sourceSegments[_currentSegment].Text;
			TargetSegment = _targetSegments[_currentSegment].Text;
			_goToNextSegmentCommand.UpdateCanExecute();
			_goToPrevSegmentCommand.UpdateCanExecute();
			StartSegmentTranslation();
		}

		public ICommand GoToPrevSegmentCommand
		{
			get { return _goToPrevSegmentCommand; }
		}

		private bool CanGoToPrevSegment()
		{
			return _sourceSegments.Count > 0 && _currentSegment > 0;
		}

		private void GoToPrevSegment()
		{
			EndSegmentTranslation();
			_currentSegment--;
			SourceSegment = _sourceSegments[_currentSegment].Text;
			TargetSegment = _targetSegments[_currentSegment].Text;
			_goToNextSegmentCommand.UpdateCanExecute();
			_goToPrevSegmentCommand.UpdateCanExecute();
			StartSegmentTranslation();
		}

		private void ResetSegment()
		{
			EndSegmentTranslation();
			_currentSegment = -1;
			SourceSegment = "";
			TargetSegment = "";
			_goToNextSegmentCommand.UpdateCanExecute();
			_goToPrevSegmentCommand.UpdateCanExecute();
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
			int i = Math.Max(0, _translator.Prefix.Count - (_translator.IsLastWordPartial ? 1 : 0));
			bool inPhrase = false;
			while (i < Math.Min(_translator.Translation.Count, _translator.Prefix.Count + lookaheadCount) || inPhrase)
			{
				string word = _translator.Translation[i];
				if (IsCapitalCase(_sourceSegmentWords[_translator.GetSourceWordIndex(i)]))
					word = ToCapitalCase(word);
				bool isPunct = word.All(char.IsPunctuation);
				if ((_translator.IsWordTransferred(i) || _translator.GetWordConfidence(i) >= 0.2f) && !isPunct)
				{
					if ((suggestions.Count == 0 || suggestions[suggestions.Count - 1].Text != word)
					    && (suggestions.Count > 0 || !TargetSegment.EndsWith(word)))
					{
						suggestions.Add(new SuggestionViewModel(this, word));
					}
					inPhrase = true;
				}
				else
				{
					inPhrase = false;
				}
				i++;
			}

			_suggestions.ReplaceAll(suggestions);
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
			if (_translator != null)
			{
				if (TargetSegment != _targetSegments[_currentSegment].Text)
				{
					_translator.Approve();
					_targetSegments[_currentSegment].Text = TargetSegment;
					TargetText = GenerateText(_targetSegments);
				}
				_translator = null;
			}
			_sourceSegmentWords.Clear();
			CurrentTargetSegmentIndex = 0;
		}

		public ICommand CloseCommand
		{
			get { return _closeCommand; }
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
				if (Set(() => TargetSegment, ref _targetSegment, value) && _translator != null)
					UpdatePrefix();
			}
		}

		public int CurrentTargetSegmentIndex
		{
			get { return _currentTargetSegmentIndex; }
			set
			{
				if (Set(() => CurrentTargetSegmentIndex, ref _currentTargetSegmentIndex, value) && _translator != null)
					UpdateSourceSegmentSelection();
			}
		}

		private void UpdateSourceSegmentSelection()
		{
			int targetWordIndex = TokenizeRegex.Matches(TargetSegment).Cast<Match>()
				.IndexOf(m => _currentTargetSegmentIndex >= m.Index && _currentTargetSegmentIndex <= m.Index + m.Length);
			if (targetWordIndex != -1)
			{
				int sourceWordIndex = _translator.GetSourceWordIndex(targetWordIndex);
				Match match = TokenizeRegex.Matches(SourceSegment)[sourceWordIndex];
				SourceSegmentSelection = new Range<int>(match.Index, match.Index + match.Length);
			}
		}

		public Range<int> SourceSegmentSelection
		{
			get { return _sourceSegmentSelection; }
			set { Set(() => SourceSegmentSelection, ref _sourceSegmentSelection, value); }
		}

		private void UpdatePrefix()
		{
			_translator.SetPrefix(TokenizeRegex.Matches(TargetSegment).Cast<Match>().Select(m => m.Value.ToLowerInvariant()), !TargetSegment.EndsWith(" "));
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

		private void ApplyAllSuggestions()
		{
			foreach (SuggestionViewModel suggestion in _suggestions.ToArray())
				suggestion.InsertSuggestion();
		}
	}
}
