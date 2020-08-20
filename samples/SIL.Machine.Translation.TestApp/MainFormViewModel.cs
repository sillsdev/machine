using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Eto.Forms;
using GalaSoft.MvvmLight;
using SIL.Machine.Corpora;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation.Thot;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.TestApp
{
	public class MainFormViewModel : ViewModelBase
	{
		private readonly IRangeTokenizer<string, int, string> _tokenizer;
		private readonly RelayCommand _openProjectCommand;
		private readonly RelayCommand _saveProjectCommand;
		private HybridTranslationEngine _hybridEngine;
		private ThotSmtModel _smtModel;
		private ITextCorpus _sourceCorpus;
		private ITextCorpus _targetCorpus;
		private ITextAlignmentCorpus _alignmentCorpus;
		private readonly TraceManager _hcTraceManager;
		private int _confidenceThreshold;
		private readonly BulkObservableList<TextViewModel> _texts;
		private TextViewModel _currentText;
		private bool _isChanged;
		private bool _isRebuilding;

		public MainFormViewModel()
		{
			_tokenizer = new LatinWordTokenizer();
			_openProjectCommand = new RelayCommand(OpenProject);
			_saveProjectCommand = new RelayCommand(SaveProject, CanSaveProject);
			CloseCommand = new RelayCommand(Close, CanClose);
			_hcTraceManager = new TraceManager();
			_confidenceThreshold = 20;
			_texts = new BulkObservableList<TextViewModel>();
			Texts = new ReadOnlyObservableList<TextViewModel>(_texts);
			_currentText = new TextViewModel(_tokenizer);
			RebuildTask = new TaskViewModel(RebuildAsync, () => _hybridEngine != null);
		}

		private void TextPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(TextViewModel.IsChanged):
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
				DialogResult result = MessageBox.Show(
					"Do you wish to save the current project before opening another project?",
					MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
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

			using (var dialog = new OpenFileDialog
				{
					Title = "Open Project",
					CheckFileExists = true,
					Filters = { new FileFilter("Project files", ".catx") }
				})
			{
				if (dialog.ShowDialog(null) == DialogResult.Ok)
				{
					CloseProject();
					if (!LoadProject(dialog.FileName))
					{
						CloseProject();
						MessageBox.Show("There was an error loading the project configuration file.",
							MessageBoxButtons.OK, MessageBoxType.Error);
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

			string configDir = Path.GetDirectoryName(fileName);
			Debug.Assert(configDir != null);

			ITranslationEngine transferEngine = null;
			if (hcSrcConfig != null && hcTrgConfig != null)
			{
				Language srcLang = XmlLanguageLoader.Load(Path.Combine(configDir, hcSrcConfig));
				var srcMorpher = new Morpher(_hcTraceManager, srcLang);

				Language trgLang = XmlLanguageLoader.Load(Path.Combine(configDir, hcTrgConfig));
				var trgMorpher = new Morpher(_hcTraceManager, trgLang);

				transferEngine = new TransferEngine(srcMorpher,
					new SimpleTransferer(new GlossMorphemeMapper(trgMorpher)), trgMorpher);
			}

			_smtModel = new ThotSmtModel(Path.Combine(configDir, smtConfig));
			IInteractiveSmtEngine smtEngine = _smtModel.CreateInteractiveEngine();

			_hybridEngine = new HybridTranslationEngine(smtEngine, transferEngine);

			var sourceTexts = new List<IText>();
			var targetTexts = new List<IText>();
			var alignmentCollections = new List<ITextAlignmentCollection>();
			using (_texts.BulkUpdate())
			{
				foreach (XElement textElem in projectElem.Elements("Texts").Elements("Text"))
				{
					var name = (string) textElem.Attribute("name");

					var metadataFileName = (string) textElem.Element("MetadataFile");
					if (metadataFileName == null)
						return false;
					metadataFileName = Path.Combine(configDir, metadataFileName);

					var srcTextFileName = (string) textElem.Element("SourceFile");
					if (srcTextFileName == null)
						return false;
					srcTextFileName = Path.Combine(configDir, srcTextFileName);

					var trgTextFileName = (string) textElem.Element("TargetFile");
					if (trgTextFileName == null)
						return false;
					trgTextFileName = Path.Combine(configDir, trgTextFileName);

					var alignmentsFileName = (string) textElem.Element("AlignmentsFile");
					if (alignmentsFileName != null)
						alignmentsFileName = Path.Combine(configDir, alignmentsFileName);

					var text = new TextViewModel(_tokenizer, name, metadataFileName, srcTextFileName, trgTextFileName)
					{
						Engine = _hybridEngine
					};
					text.PropertyChanged += TextPropertyChanged;
					_texts.Add(text);

					Func<TextSegment, bool> segmentFilter = s => text.IsApproved((TextSegmentRef) s.SegmentRef);
					sourceTexts.Add(new FilteredText(new TextFileText(_tokenizer, name, srcTextFileName),
						segmentFilter));
					targetTexts.Add(new FilteredText(new TextFileText(_tokenizer, name, trgTextFileName),
						segmentFilter));
					if (alignmentsFileName != null)
						alignmentCollections.Add(new TextFileTextAlignmentCollection(name, alignmentsFileName));
				}
			}
			if (_texts.Count == 0)
				return false;

			_sourceCorpus = new DictionaryTextCorpus(sourceTexts);
			_targetCorpus = new DictionaryTextCorpus(targetTexts);
			_alignmentCorpus = new DictionaryTextAlignmentCorpus(alignmentCollections);

			CurrentText = _texts[0];
			AcceptChanges();
			RebuildTask.UpdateCanExecute();
			return true;
		}

		public ICommand SaveProjectCommand => _saveProjectCommand;

		private bool CanSaveProject()
		{
			return _isChanged && !_isRebuilding;
		}

		private void SaveProject()
		{
			_smtModel.Save();
			foreach (TextViewModel text in _texts)
				text.Save();
			AcceptChanges();
		}

		private void CloseProject()
		{
			CurrentText = null;
			_texts.Clear();
			CurrentText = new TextViewModel(_tokenizer);
			_sourceCorpus = null;
			_targetCorpus = null;
			if (_hybridEngine != null)
			{
				_hybridEngine.Dispose();
				_hybridEngine = null;
			}
			if (_smtModel != null)
			{
				_smtModel.Dispose();
				_smtModel = null;
			}
			_saveProjectCommand.UpdateCanExecute();
			RebuildTask.UpdateCanExecute();
		}

		private async Task RebuildAsync(IProgress<ProgressStatus> progress, CancellationToken token)
		{
			if (IsChanged)
				SaveProject();

			_isRebuilding = true;
			try
			{
				using (ISmtBatchTrainer trainer = _smtModel.CreateBatchTrainer(TokenProcessors.Lowercase, _sourceCorpus,
					TokenProcessors.Lowercase, _targetCorpus, _alignmentCorpus))
				{
					await Task.Run(() => trainer.Train(progress, token.ThrowIfCancellationRequested), token);
					trainer.Save();
				}
			}
			catch (OperationCanceledException)
			{
				// the user canceled rebuilding
			}
			finally
			{
				_isRebuilding = false;
				_saveProjectCommand.UpdateCanExecute();
			}
		}

		public ICommand CloseCommand { get; }

		private bool CanClose()
		{
			if (IsChanged)
			{
				DialogResult result = MessageBox.Show("Do you wish to save the project before exiting?",
					MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
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
			get => _confidenceThreshold;
			set
			{
				if (Set(nameof(ConfidenceThreshold), ref _confidenceThreshold, value))
					_currentText.ConfidenceThreshold = _confidenceThreshold / 100.0;
			}
		}

		public ReadOnlyObservableList<TextViewModel> Texts { get; }

		public TextViewModel CurrentText
		{
			get => _currentText;
			set
			{
				TextViewModel oldText = _currentText;
				if (Set(nameof(CurrentText), ref _currentText, value))
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

		public TaskViewModel RebuildTask { get; }
	}
}
