using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab;
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
		}

		public IEnumerable<EngineContext> GetAll()
		{
			lock (_engines)
				return _engines.Values.ToArray();
		}

		public bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineContext engineContext)
		{
			lock (_engines)
				return _engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext);
		}

		public async Task<SessionContext> TryCreateSession(string sourceLanguageTag, string targetLanguageTag)
		{
			EngineContext engineContext;
			lock (_engines)
			{
				if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
					return null;
			}

			using (await engineContext.Mutex.LockAsync().ConfigureAwait(false))
			{
				if (engineContext.Engine == null)
					engineContext.Engine = await Task.Run(() => LoadEngine(sourceLanguageTag, targetLanguageTag)).ConfigureAwait(false);
				Debug.Assert(engineContext.Engine != null);
				string id = Guid.NewGuid().ToString();
				var sessionContext = new SessionContext(id, engineContext, engineContext.Engine.StartSession());
				_sessionService.Add(sessionContext);
				return sessionContext;
			}
		}

		public async Task<TranslationResult> TryTranslate(string sourceLanguageTag, string targetLanguageTag, string segment)
		{
			EngineContext engineContext;
			lock (_engines)
			{
				if (!_engines.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out engineContext))
					return null;
			}

			using (await engineContext.Mutex.LockAsync().ConfigureAwait(false))
			{
				if (engineContext.Engine == null)
					engineContext.Engine = await Task.Run(() => LoadEngine(sourceLanguageTag, targetLanguageTag)).ConfigureAwait(false);
				Debug.Assert(engineContext.Engine != null);
				return engineContext.Engine.Translate(segment.Tokenize());
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
			lock (_engines)
			{
				foreach (EngineContext engineContext in _engines.Values)
				{
					using (engineContext.Mutex.Lock())
						engineContext.Engine.Dispose();
				}
				_engines.Clear();
			}
		}
	}
}
