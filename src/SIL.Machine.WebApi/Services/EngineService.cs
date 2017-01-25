using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Services
{
	public class EngineService : DisposableBase
	{
		private readonly EngineOptions _options;
		private readonly ConcurrentDictionary<Tuple<string, string>, EngineContext> _engines;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly ITranslationEngineFactory _ruleEngineFactory;
		private readonly Timer _updateTimer;
		private bool _isUpdateTimerStopped;

		public EngineService(IOptions<EngineOptions> options, ISmtModelFactory smtModelFactory, ITranslationEngineFactory ruleEngineFactory)
		{
			_options = options.Value;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_engines = new ConcurrentDictionary<Tuple<string, string>, EngineContext>();
			foreach (string configDir in Directory.EnumerateDirectories(_options.RootDir))
			{
				string dirName = Path.GetFileName(configDir);
				string[] parts = dirName.Split('_');
				if (parts.Length > 2)
					continue;
				string sourceLanguageTag = parts[0];
				string targetLanguageTag = parts[1];
				_engines[Tuple.Create(sourceLanguageTag, targetLanguageTag)] = new EngineContext(configDir, sourceLanguageTag, targetLanguageTag);
			}
			_updateTimer = new Timer(UpdateEnginesCallback, null, _options.EngineUpdateFrequency, _options.EngineUpdateFrequency);
		}

		private void UpdateEnginesCallback(object state)
		{
			if (_isUpdateTimerStopped)
				return;

			foreach (EngineContext engineContext in _engines.Values)
			{
				lock (engineContext)
				{
					if (engineContext.IsRemoved)
						continue;

					if (engineContext.IsLoaded)
					{
						if (DateTime.Now - engineContext.LastUsedTime > _options.InactiveEngineTimeout)
							engineContext.Unload();
						else
							engineContext.Save();
					}
				}
			}
		}

		public IEnumerable<EngineDto> GetAll()
		{
			foreach (EngineContext engineContext in _engines.Values)
			{
				lock (engineContext)
				{
					if (!engineContext.IsRemoved)
						yield return engineContext.CreateDto();
				}
			}
		}

		public bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineDto engine)
		{
			EngineContext engineContext;
			if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
			{
				engine = null;
				return false;
			}

			lock (engineContext)
			{
				if (engineContext.IsRemoved)
				{
					engine = null;
					return false;
				}

				engine = engineContext.CreateDto();
				return true;
			}
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
				if (engineContext.IsRemoved)
				{
					result = null;
					return false;
				}

				if (!engineContext.IsLoaded)
					engineContext.Load(_smtModelFactory, _ruleEngineFactory);

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
				if (engineContext.IsRemoved)
				{
					result = null;
					return false;
				}

				if (!engineContext.IsLoaded)
					engineContext.Load(_smtModelFactory, _ruleEngineFactory);

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
				if (engineContext.IsRemoved)
					return false;

				if (!engineContext.IsLoaded)
					engineContext.Load(_smtModelFactory, _ruleEngineFactory);

				string[] sourceSegment = engineContext.Tokenizer.TokenizeToStrings(segmentPair.SourceSegment).ToArray();
				string[] targetSegment = engineContext.Tokenizer.TokenizeToStrings(segmentPair.TargetSegment).ToArray();
				engineContext.Engine.TrainSegment(sourceSegment.Select(s => s.ToLowerInvariant()), targetSegment.Select(s => s.ToLowerInvariant()));
				engineContext.MarkUpdated();
				engineContext.LastUsedTime = DateTime.Now;
				return true;
			}
		}

		public bool Add(string sourceLanguageTag, string targetLanguageTag, out EngineDto engine)
		{
			string configDir = Path.Combine(_options.RootDir, $"{sourceLanguageTag}_{targetLanguageTag}");
			if (!Directory.Exists(configDir))
			{
				engine = null;
				return false;
			}

			EngineContext engineContext = _engines.GetOrAdd(Tuple.Create(sourceLanguageTag, targetLanguageTag),
				k => new EngineContext(configDir, sourceLanguageTag, targetLanguageTag));
			lock (engineContext)
			{
				if (engineContext.IsRemoved)
				{
					engine = null;
					return false;
				}

				engine = engineContext.CreateDto();
				return true;
			}
		}

		public bool Remove(string sourceLanguageTag, string targetLanguageTag)
		{
			EngineContext engineContext;
			if (_engines.TryRemove(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
			{
				lock (engineContext)
				{
					if (engineContext.IsLoaded)
						engineContext.Unload();
					engineContext.MarkRemoved();
				}
				return true;
			}

			return false;
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
					engineContext.MarkRemoved();
				}
			}
		}
	}
}
