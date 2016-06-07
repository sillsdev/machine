using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class EngineService : DisposableBase, IEngineService
	{
		private readonly EngineOptions _options;
		private readonly Dictionary<Tuple<string, string>, EngineContext> _engines;
		private readonly ISessionService _sessionService;
		private readonly Timer _cleanupTimer;
		private bool _isTimerStopped;

		public EngineService(IOptions<EngineOptions> options, ISessionService sessionService)
		{
			_options = options.Value;
			_sessionService = sessionService;
			_engines = new Dictionary<Tuple<string, string>, EngineContext>();
			foreach (string configDir in Directory.EnumerateDirectories(_options.RootDir))
			{
				string dirName = Path.GetFileName(configDir);
				string[] parts = dirName.Split('_');
				string sourceLanguageTag = parts[0];
				string targetLanguageTag = parts[1];
				Tuple<string, string> key = Tuple.Create(sourceLanguageTag, targetLanguageTag);
				_engines[key] = new EngineContext(sourceLanguageTag, targetLanguageTag);
			}
			_cleanupTimer = new Timer(CleanupUnusedEngines, null, _options.UnusedEngineCleanupFrequency, _options.UnusedEngineCleanupFrequency);
		}

		private void CleanupUnusedEngines(object state)
		{
			if (_isTimerStopped)
				return;

			foreach (EngineContext engineContext in _engines.Values)
			{
				lock (engineContext)
				{
					if (engineContext.Engine != null && engineContext.SessionCount == 0)
					{
						engineContext.Engine.Dispose();
						engineContext.Engine = null;
					}
				}
			}
		}

		public IEnumerable<EngineContext> GetAll()
		{
			return _engines.Values.ToArray();
		}

		public bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineContext engineContext)
		{
			return _engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext);
		}

		public bool TryCreateSession(string sourceLanguageTag, string targetLanguageTag, out SessionContext sessionContext)
		{
			EngineContext engineContext;
			if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
			{
				sessionContext = null;
				return false;
			}

			lock (engineContext)
			{
				if (engineContext.Engine == null)
					engineContext.Engine = LoadEngine(sourceLanguageTag, targetLanguageTag);
				Debug.Assert(engineContext.Engine != null);
				string id = Guid.NewGuid().ToString();
				sessionContext = new SessionContext(id, engineContext, engineContext.Engine.StartSession());
				engineContext.SessionCount++;
			}
			_sessionService.Add(sessionContext);
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
				if (engineContext.Engine == null)
					engineContext.Engine = LoadEngine(sourceLanguageTag, targetLanguageTag);
				Debug.Assert(engineContext.Engine != null);
				TranslationResult translationResult = engineContext.Engine.Translate(engineContext.Tokenizer.TokenizeToStrings(segment));
				result = engineContext.Detokenizer.Detokenize(Enumerable.Range(0, translationResult.TargetSegment.Count).Select(j => translationResult.RecaseTargetWord(j)));
				return true;
			}
		}

		private HybridTranslationEngine LoadEngine(string sourceLanguageTag, string targetLanguageTag)
		{
			string configDir = Path.Combine(_options.RootDir, string.Format("{0}_{1}", sourceLanguageTag, targetLanguageTag));
			string smtConfigFileName = Path.Combine(configDir, "smt.cfg");
			var smtEngine = new ThotSmtEngine(smtConfigFileName);

			string hcSrcConfigFileName = Path.Combine(configDir, string.Format("{0}-hc.xml", sourceLanguageTag));
			string hcTrgConfigFileName = Path.Combine(configDir, string.Format("{0}-hc.xml", targetLanguageTag));
			TransferEngine transferEngine = null;
			if (File.Exists(hcSrcConfigFileName) && File.Exists(hcTrgConfigFileName))
			{
				var spanFactory = new ShapeSpanFactory();
				var hcTraceManager = new TraceManager();

				Language srcLang = XmlLoader.Load(hcSrcConfigFileName);
				var srcMorpher = new Morpher(spanFactory, hcTraceManager, srcLang);
				var srcAnalyzer = new HermitCrabMorphologicalAnalyzer(GetMorphemeId, GetCategory, srcMorpher);

				Language trgLang = XmlLoader.Load(hcTrgConfigFileName);
				var trgMorpher = new Morpher(spanFactory, hcTraceManager, trgLang);
				var trgGenerator = new HermitCrabMorphologicalGenerator(GetMorphemeId, GetCategory, trgMorpher);

				transferEngine = new TransferEngine(srcAnalyzer, new SimpleTransferer(new GlossMorphemeMapper(trgGenerator)), trgGenerator);
			}

			return new HybridTranslationEngine(smtEngine, transferEngine);
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

		protected override void DisposeManagedResources()
		{
			_isTimerStopped = true;
			_cleanupTimer.Dispose();

			foreach (EngineContext engineContext in _engines.Values)
			{
				lock (engineContext)
				{
					engineContext.Engine.Dispose();
					engineContext.Engine = null;
				}
			}
		}
	}
}
