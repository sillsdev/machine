using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Xml.Linq;
using Eto.Forms;
using GalaSoft.MvvmLight;
using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation.Thot;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.TestApp
{
	public class MainFormViewModel : ViewModelBase
	{
		private readonly RegexTokenizer _tokenizer;
		private readonly RelayCommand<object> _openProjectCommand;
		private readonly RelayCommand<object> _saveProjectCommand;
		private readonly RelayCommand<object> _rebuildProjectCommand; 
		private HybridTranslationEngine _engine;
		private ThotSmtModel _smtModel;
		private ITextCorpus _sourceCorpus;
		private ITextCorpus _targetCorpus;
		private readonly ShapeSpanFactory _spanFactory;
		private readonly TraceManager _hcTraceManager;
		private int _confidenceThreshold;
		private readonly BulkObservableList<TextViewModel> _texts;
		private TextViewModel _currentText;
		private bool _isChanged;

		public MainFormViewModel()
		{
			_tokenizer = new RegexTokenizer(new IntegerSpanFactory(), @"[\p{P}]|(\w+([.,\-’']\w+)*)");
			_openProjectCommand = new RelayCommand<object>(o => OpenProject());
			_saveProjectCommand = new RelayCommand<object>(o => SaveProject(), o => IsChanged);
			_rebuildProjectCommand = new RelayCommand<object>(o => RebuildProject(), o => CanRebuildProject());
			CloseCommand = new RelayCommand<object>(o => Close(), o => CanClose());
			_spanFactory = new ShapeSpanFactory();
			_hcTraceManager = new TraceManager();
			_confidenceThreshold = 20;
			_texts = new BulkObservableList<TextViewModel>();
			Texts = new ReadOnlyObservableList<TextViewModel>(_texts);
			_currentText = new TextViewModel(_tokenizer);
		}

		private void TextPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "IsChanged":
					if (((TextViewModel) sender).IsChanged)
						IsChanged = true;
					break;
			}
		}

		private bool IsChanged
		{
			get { return _isChanged; }
			set
			{
				if (_isChanged != value)
				{
					_isChanged = value;
					_saveProjectCommand.UpdateCanExecute();
				}
			}
		}

		private void AcceptChanges()
		{
			IsChanged = false;
			foreach (TextViewModel text in _texts)
				text.AcceptChanges();
		}

		public ICommand OpenProjectCommand => _openProjectCommand;

		private void OpenProject()
		{
			if (IsChanged)
			{
				DialogResult result = MessageBox.Show("Do you wish to save the current project before opening another project?", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
				switch (result)
				{
					case DialogResult.Yes:
						SaveProject();
						break;

					case DialogResult.No:
						break;

					case DialogResult.Cancel:
						return;
				}
			}

			using (var dialog = new OpenFileDialog {Title = "Open Project", CheckFileExists = true, Filters = {new FileDialogFilter("Project files", ".catx")}})
			{
				if (dialog.ShowDialog(null) == DialogResult.Ok)
				{
					CloseProject();
					if (!LoadProject(dialog.FileName))
					{
						CloseProject();
						MessageBox.Show("There was an error loading the project configuration file.", MessageBoxButtons.OK, MessageBoxType.Error);
					}
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

			string configDir = Path.GetDirectoryName(fileName) ?? "";

			TransferEngine transferEngine = null;
			if (hcSrcConfig != null && hcTrgConfig != null)
			{
				Language srcLang = XmlLanguageLoader.Load(Path.Combine(configDir, hcSrcConfig));
				var srcMorpher = new Morpher(_spanFactory, _hcTraceManager, srcLang);

				Language trgLang = XmlLanguageLoader.Load(Path.Combine(configDir, hcTrgConfig));
				var trgMorpher = new Morpher(_spanFactory, _hcTraceManager, trgLang);

				transferEngine = new TransferEngine(srcMorpher, new SimpleTransferer(new GlossMorphemeMapper(trgMorpher)), trgMorpher);
			}

			_smtModel = new ThotSmtModel(Path.Combine(configDir, smtConfig));

			_engine = new HybridTranslationEngine(_smtModel.CreateEngine(), transferEngine);

			var sourceTexts = new List<IText>();
			var targetTexts = new List<IText>();
			using (_texts.BulkUpdate())
			{
				foreach (XElement textElem in projectElem.Elements("Texts").Elements("Text"))
				{
					var name = (string) textElem.Attribute("name");

					var srcTextFile = (string) textElem.Element("SourceFile");
					if (srcTextFile == null)
						return false;

					var trgTextFile = (string) textElem.Element("TargetFile");
					if (trgTextFile == null)
						return false;

					var text = new TextViewModel(_tokenizer, name, Path.Combine(configDir, srcTextFile), Path.Combine(configDir, trgTextFile), _engine);
					text.PropertyChanged += TextPropertyChanged;
					_texts.Add(text);

					sourceTexts.Add(new TextAdapter(text, true));
					targetTexts.Add(new TextAdapter(text, false));
				}
			}
			if (_texts.Count == 0)
				return false;

			_sourceCorpus = new DictionaryTextCorpus(sourceTexts);
			_targetCorpus = new DictionaryTextCorpus(targetTexts);

			_engine.SourcePreprocessor = Preprocess;
			_engine.SourceTokenizer = _tokenizer;
			_engine.TargetPreprocessor = Preprocess;
			_engine.TargetTokenizer = _tokenizer;

			CurrentText = _texts[0];
			AcceptChanges();
			_rebuildProjectCommand.UpdateCanExecute();
			return true;
		}

		public ICommand SaveProjectCommand => _saveProjectCommand;

		private void SaveProject()
		{
			_smtModel.Save();
			foreach (TextViewModel text in _texts)
				text.SaveTargetText();
			AcceptChanges();
		}

		private void CloseProject()
		{
			CurrentText = null;
			_texts.Clear();
			CurrentText = new TextViewModel(_tokenizer);
			_sourceCorpus = null;
			_targetCorpus = null;
			if (_engine != null)
			{
				_engine.Dispose();
				_engine = null;
			}
			if (_smtModel != null)
			{
				_smtModel.Dispose();
				_smtModel = null;
			}
			_saveProjectCommand.UpdateCanExecute();
			_rebuildProjectCommand.UpdateCanExecute();
		}

		public ICommand RebuildProjectCommand => _rebuildProjectCommand;

		private bool CanRebuildProject()
		{
			return _engine != null;
		}

		private void RebuildProject()
		{
			_currentText.IsActive = false;
			if (IsChanged)
				SaveProject();
			var progressViewModel = new ProgressViewModel(vm => _smtModel.Train(Preprocess, _tokenizer, _sourceCorpus, Preprocess, _tokenizer, _targetCorpus, vm))
			{
				DisplayName = "Rebuilding..."
			};
			using (var progressDialog = new ProgressDialog())
			{
				progressDialog.DataContext = progressViewModel;
				progressDialog.ShowModal();
			}
			_currentText.IsActive = true;
		}

		public ICommand CloseCommand { get; }

		private bool CanClose()
		{
			if (IsChanged)
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

		public int ConfidenceThreshold
		{
			get { return _confidenceThreshold; }
			set
			{
				if (Set(() => ConfidenceThreshold, ref _confidenceThreshold, value))
					_currentText.ConfidenceThreshold = _confidenceThreshold / 100.0;
			}
		}

		public ReadOnlyObservableList<TextViewModel> Texts { get; }

		public TextViewModel CurrentText
		{
			get { return _currentText; }
			set
			{
				TextViewModel oldText = _currentText;
				if (Set(() => CurrentText, ref _currentText, value))
				{
					if (oldText != null)
						oldText.IsActive = false;
					if (_currentText != null)
					{
						_currentText.ConfidenceThreshold = _confidenceThreshold / 100.0;
						_currentText.IsActive = true;
					}
				}
			}
		}

		private static string Preprocess(string str)
		{
			return str.ToLowerInvariant();
		}
	}
}
