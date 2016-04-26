using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
		private static readonly TextViewModel EmptyText = new TextViewModel();

		private readonly RelayCommand<object> _openProjectCommand;
		private readonly RelayCommand<object> _saveProjectCommand;
		private readonly RelayCommand<object> _closeCommand;
		private ISmtEngine _smtEngine;
		private TranslationEngine _translationEngine;
		private readonly ShapeSpanFactory _spanFactory;
		private readonly TraceManager _hcTraceManager;
		private int _confidenceThreshold;
		private readonly BulkObservableList<TextViewModel> _texts;
		private readonly ReadOnlyObservableList<TextViewModel> _readOnlyTexts;
		private TextViewModel _currentText;
		private bool _isChanged;

		public MainFormViewModel()
		{
			_openProjectCommand = new RelayCommand<object>(o => OpenProject());
			_saveProjectCommand = new RelayCommand<object>(o => SaveProject(), o => IsChanged);
			_closeCommand = new RelayCommand<object>(o => Close(), o => CanClose());
			_spanFactory = new ShapeSpanFactory();
			_hcTraceManager = new TraceManager();
			_confidenceThreshold = 20;
			_texts = new BulkObservableList<TextViewModel>();
			_readOnlyTexts = new ReadOnlyObservableList<TextViewModel>(_texts);
			_currentText = EmptyText;
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

		public ICommand OpenProjectCommand
		{
			get { return _openProjectCommand; }
		}

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
				Language srcLang = XmlLoader.Load(Path.Combine(configDir, hcSrcConfig));
				var srcMorpher = new Morpher(_spanFactory, _hcTraceManager, srcLang);
				var srcAnalyzer = new HermitCrabSourceAnalyzer(GetMorphemeId, GetCategory, srcMorpher);

				Language trgLang = XmlLoader.Load(Path.Combine(configDir, hcTrgConfig));
				var trgMorpher = new Morpher(_spanFactory, _hcTraceManager, trgLang);
				var trgGenerator = new HermitCrabTargetGenerator(GetMorphemeId, GetCategory, trgMorpher);

				transferEngine = new TransferEngine(srcAnalyzer, new SimpleTransferer(new GlossMorphemeMapper(trgGenerator)), trgGenerator);
			}
			_smtEngine = new ThotSmtEngine(Path.Combine(configDir, smtConfig));
			_translationEngine = new TranslationEngine(_smtEngine, transferEngine);

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

					var text = new TextViewModel(_translationEngine, name, Path.Combine(configDir, srcTextFile), Path.Combine(configDir, trgTextFile));
					text.PropertyChanged += TextPropertyChanged;
					_texts.Add(text);
				}
			}
			if (_texts.Count == 0)
				return false;

			CurrentText = _texts[0];

			AcceptChanges();
			return true;
		}

		public ICommand SaveProjectCommand
		{
			get { return _saveProjectCommand; }
		}

		private void SaveProject()
		{
			_smtEngine.SaveModels();
			foreach (TextViewModel text in _texts)
				text.SaveTargetText();
			AcceptChanges();
		}

		private void CloseProject()
		{
			if (_translationEngine != null)
			{
				_translationEngine.Dispose();
				_translationEngine = null;
			}
			if (_smtEngine != null)
			{
				_smtEngine.Dispose();
				_smtEngine = null;
			}
			CurrentText = null;
			_texts.Clear();
			CurrentText = EmptyText;
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

		public ICommand CloseCommand
		{
			get { return _closeCommand; }
		}

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

		public ReadOnlyObservableList<TextViewModel> Texts
		{
			get { return _readOnlyTexts; }
		}

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
						_currentText.IsActive = true;
						_currentText.ConfidenceThreshold = _confidenceThreshold / 100.0;
					}
				}
			}
		}
	}
}
