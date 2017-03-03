using System;
using System.Collections.Generic;
using System.IO;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Services;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class Engine : DisposableBase
	{
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly ITranslationEngineFactory _ruleEngineFactory;
		private readonly TimeSpan _inactiveTimeout;
		private IInteractiveSmtModel _smtModel;
		private IInteractiveSmtEngine _smtEngine;
		private ITranslationEngine _ruleEngine;
		private HybridTranslationEngine _hybridEngine;
		private bool _isLoaded;
		private bool _isUpdated;
		private DateTime _lastUsedTime;

		public Engine(ISmtModelFactory smtModelFactory, ITranslationEngineFactory ruleEngineFactory, TimeSpan inactiveTimeout, string configDir,
			string sourceLanguageTag, string targetLanguageTag)
		{
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_inactiveTimeout = inactiveTimeout;
			ConfigDirectory = configDir;
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			_lastUsedTime = DateTime.Now;
			Projects = new HashSet<Project>();

			if (!Directory.Exists(ConfigDirectory))
			{
				Directory.CreateDirectory(ConfigDirectory);
				_smtModelFactory.InitNewModel(this);
			}
		}

		public string ConfigDirectory { get; }
		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public ISet<Project> Projects { get; }

		private void Save()
		{
			if (_isUpdated)
			{
				_smtModel.Save();
				_isUpdated = false;
			}
		}

		private void Load()
		{
			if (_isLoaded)
				return;

			_smtModel = _smtModelFactory.Create(this);
			_smtEngine = _smtModel.CreateInteractiveEngine();

			_ruleEngine = _ruleEngineFactory.Create(this);

			_hybridEngine = new HybridTranslationEngine(_smtEngine, _ruleEngine);
			_isLoaded = true;
		}

		private void Unload()
		{
			if (!_isLoaded)
				return;

			Save();

			_hybridEngine.Dispose();
			_hybridEngine = null;

			if (_ruleEngine != null)
			{
				_ruleEngine.Dispose();
				_ruleEngine = null;
			}

			_smtEngine.Dispose();
			_smtEngine = null;
			_smtModel.Dispose();
			_smtModel = null;
			_isLoaded = false;
		}

		public TranslationResult Translate(IReadOnlyList<string> segment)
		{
			Load();

			TranslationResult result = _hybridEngine.Translate(segment);
			_lastUsedTime = DateTime.Now;
			return result;
		}

		public void InteractiveTranslate(IReadOnlyList<string> segment, out WordGraph smtWordGraph, out TranslationResult ruleResult)
		{
			Load();

			smtWordGraph = _smtEngine.GetWordGraph(segment);
			ruleResult = _ruleEngine?.Translate(segment);
			_lastUsedTime = DateTime.Now;
		}

		public void TrainSegmentPair(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			Load();

			_hybridEngine.TrainSegment(sourceSegment, targetSegment);
			_isUpdated = true;
			_lastUsedTime = DateTime.Now;
		}

		public void Commit()
		{
			if (!_isLoaded)
				return;

			if (DateTime.Now - _lastUsedTime > _inactiveTimeout)
				Unload();
			else
				Save();
		}

		protected override void DisposeManagedResources()
		{
			Unload();
		}
	}
}
