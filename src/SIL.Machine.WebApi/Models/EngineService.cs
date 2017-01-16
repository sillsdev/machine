using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class EngineService : DisposableBase, IEngineService
	{
		private readonly EngineOptions _options;
		private readonly ConcurrentDictionary<Tuple<string, string>, EngineContext> _engines;
		private readonly Timer _updateTimer;
		private bool _isUpdateTimerStopped;

		public EngineService(IOptions<EngineOptions> options)
		{
			_options = options.Value;
			_engines = new ConcurrentDictionary<Tuple<string, string>, EngineContext>();
			UpdateEngines();
			_updateTimer = new Timer(UpdateEnginesCallback, null, _options.EngineUpdateFrequency, _options.EngineUpdateFrequency);
		}

		private void UpdateEnginesCallback(object state)
		{
			if (_isUpdateTimerStopped)
				return;

			UpdateEngines();
		}

		private void UpdateEngines()
		{
			var enginesToRemove = new HashSet<Tuple<string, string>>(_engines.Keys);
			foreach (string configDir in Directory.EnumerateDirectories(_options.RootDir))
			{
				string dirName = Path.GetFileName(configDir);
				string[] parts = dirName.Split('_');
				string sourceLanguageTag = parts[0];
				string targetLanguageTag = parts[1];
				Tuple<string, string> key = Tuple.Create(sourceLanguageTag, targetLanguageTag);
				EngineContext engineContext = _engines.GetOrAdd(key, k => new EngineContext(configDir, sourceLanguageTag, targetLanguageTag));
				lock (engineContext)
				{
					if (engineContext.IsLoaded)
					{
						if (DateTime.Now - engineContext.LastUsedTime > _options.InactiveEngineTimeout)
							engineContext.Unload();
						else
							engineContext.Save();
					}
				}
				enginesToRemove.Remove(key);
			}

			foreach (Tuple<string, string> key in enginesToRemove)
			{
				EngineContext engineContext;
				if (_engines.TryRemove(key, out engineContext))
				{
					lock (engineContext)
					{
						if (engineContext.IsLoaded)
							engineContext.Unload();
					}
				}
			}
		}

		public IEnumerable<EngineDto> GetAll()
		{
			return _engines.Values.Select(ec => ec.CreateDto());
		}

		public bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineDto engine)
		{
			EngineContext engineContext;
			if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
			{
				engine = null;
				return false;
			}

			engine = engineContext.CreateDto();
			return true;
		}

		public bool TryTranslate(string sourceLanguageTag, string targetLanguageTag, string segment, out string result)
		{
			EngineContext engineContext;
			if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
			{
				result = null;
				return false;
			}

			lock (engineContext)
			{
				if (!engineContext.IsLoaded)
					engineContext.Load();
				string[] sourceSegment = engineContext.Tokenizer.TokenizeToStrings(segment).ToArray();
				TranslationResult translationResult = engineContext.Engine.Translate(sourceSegment.Select(w => w.ToLowerInvariant()));
				result = engineContext.Detokenizer.Detokenize(Enumerable.Range(0, translationResult.TargetSegment.Count)
					.Select(j => translationResult.RecaseTargetWord(sourceSegment, j)));
				engineContext.LastUsedTime = DateTime.Now;
				return true;
			}
		}

		public bool TryInteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, IReadOnlyList<string> segment, out InteractiveTranslationResultDto result)
		{
			EngineContext engineContext;
			if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
			{
				result = null;
				return false;
			}

			lock (engineContext)
			{
				if (!engineContext.IsLoaded)
					engineContext.Load();
				string[] sourceSegment = segment.Select(s => s.ToLowerInvariant()).ToArray();

				WordGraph smtWordGraph = engineContext.Engine.SmtEngine.GetWordGraph(sourceSegment);
				TranslationResult ruleResult = engineContext.Engine.RuleEngine?.Translate(sourceSegment);

				result = new InteractiveTranslationResultDto
				{
					WordGraph = smtWordGraph.CreateDto(segment),
					RuleResult = ruleResult?.CreateDto(segment)
				};
				engineContext.LastUsedTime = DateTime.Now;
				return true;
			}
		}

		public bool TryTrainSegment(string sourceLanguageTag, string targetLanguageTag, SegmentPairDto segmentPair)
		{
			EngineContext engineContext;
			if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
				return false;

			lock (engineContext)
			{
				if (!engineContext.IsLoaded)
					engineContext.Load();

				engineContext.Engine.TrainSegment(segmentPair.SourceSegment.Select(s => s.ToLowerInvariant()), segmentPair.TargetSegment.Select(s => s.ToLowerInvariant()));
				engineContext.MarkUpdated();
				engineContext.LastUsedTime = DateTime.Now;
				return true;
			}
		}

		protected override void DisposeManagedResources()
		{
			_isUpdateTimerStopped = true;
			_updateTimer.Dispose();

			foreach (EngineContext engineContext in _engines.Values)
			{
				lock (engineContext)
				{
					if (engineContext.IsLoaded)
						engineContext.Unload();
				}
			}
		}
	}
}
