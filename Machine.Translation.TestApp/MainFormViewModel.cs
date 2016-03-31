using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Eto.Forms;
using GalaSoft.MvvmLight;
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

		public MainFormViewModel()
		{
			_sourceSegments = new List<Segment>();
			_targetSegments = new List<Segment>();
			_paragraphs = new HashSet<int>();
			_openProjectCommand = new RelayCommand<object>(o => OpenProject());
			_goToNextSegmentCommand = new RelayCommand<object>(o => GoToNextSegment(), o => CanGoToNextSegment());
			_goToPrevSegmentCommand = new RelayCommand<object>(o => GoToPrevSegment(), o => CanGoToPrevSegment());
			_closeCommand = new RelayCommand<object>(o => Close());
			_spanFactory = new ShapeSpanFactory();
			_hcTraceManager = new TraceManager();
			_suggestions = new BulkObservableList<SuggestionViewModel>();
			_readOnlySuggestions = new ReadOnlyObservableList<SuggestionViewModel>(_suggestions);
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

			LoadTextFile(Path.Combine(configDir, srcTextFile), _sourceSegments);
			SourceText = GenerateText(_sourceSegments);
			LoadTextFile(Path.Combine(configDir, trgTextFile), _targetSegments);
			while (_targetSegments.Count < _sourceSegments.Count)
				_targetSegments.Add(new Segment());
			TargetText = GenerateText(_targetSegments);

			Language srcLang = XmlLoader.Load(Path.Combine(configDir, hcSrcConfig));
			var srcMorpher = new Morpher(_spanFactory, _hcTraceManager, srcLang);
			var srcAnalyzer = new HermitCrabSourceAnalyzer(GetMorphemeId, GetCategory, srcMorpher);

			Language trgLang = XmlLoader.Load(Path.Combine(configDir, hcTrgConfig));
			var trgMorpher = new Morpher(_spanFactory, _hcTraceManager, trgLang);
			var trgGenerator = new HermitCrabTargetGenerator(GetMorphemeId, GetCategory, trgMorpher);

			_engine = new TranslationEngine(Path.Combine(configDir, smtConfig), srcAnalyzer, new SimpleTransferer(new GlossMorphemeMapper(trgGenerator)), trgGenerator);

			if (_sourceSegments.Count > 0)
			{
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

			_sourceSegments.Clear();
			_targetSegments.Clear();
			SourceText = "";
			TargetText = "";
		}

		private void LoadTextFile(string fileName, List<Segment> segments)
		{
			foreach (string line in File.ReadAllLines(fileName))
			{
				if (line.Length > 0)
					segments.Add(new Segment {Text = line});
				else
					_paragraphs.Add(segments.Count);
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
			_translator = _engine.StartSegmentTranslation(matches.Cast<Match>().Select(m => m.Value.ToLowerInvariant()));
			UpdatePrefix();
		}

		private void UpdateSuggestions()
		{
			var suggestions = new List<SuggestionViewModel>();
			for (int i = _translator.Prefix.Count; i < Math.Min(_translator.CurrentTranslation.Count, _translator.Prefix.Count + 3) ; i++)
			{
				string word = _translator.CurrentTranslation[i];
				float confidence = _translator.WordConfidences[i];
				bool isPunct = word.All(char.IsPunctuation);
				if (confidence >= 0.1f && !isPunct)
				{
					if (suggestions.All(s => s.Text != word))
						suggestions.Add(new SuggestionViewModel(this, word));
				}
			}

			_suggestions.ReplaceAll(suggestions);
		}

		private void EndSegmentTranslation()
		{
			if (_translator != null)
			{
				if (!string.IsNullOrEmpty(TargetSegment))
				{
					_translator.Approve();
					_targetSegments[_currentSegment].Text = TargetSegment;
					TargetText = GenerateText(_targetSegments);
				}
				_translator = null;
			}
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
			set { Set(() => CurrentTargetSegmentIndex, ref _currentTargetSegmentIndex, value); }
		}

		private void UpdatePrefix()
		{
			_translator.Prefix.ReplaceAll(TokenizeRegex.Matches(TargetSegment).Cast<Match>().Select(m => m.Value.ToLowerInvariant()));
			UpdateSuggestions();
		}

		public ReadOnlyObservableList<SuggestionViewModel> Suggestions
		{
			get { return _readOnlySuggestions; }
		}
	}
}
