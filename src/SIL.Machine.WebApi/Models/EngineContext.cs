using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.WebApi.Models
{
	public class EngineContext
	{
		private static readonly HashSet<string> MergeRightTokens = new HashSet<string> {"‘", "“", "(", "¿", "¡", "«"};
		private static readonly HashSet<string> MergeRightFirstLeftSecondTokens = new HashSet<string> {"\"", "'"};

		private readonly string _configDir;
		private ThotSmtModel _smtModel;
		private bool _isUpdated;

		public EngineContext(string configDir, string sourceLanguageTag, string targetLanguageTag)
		{
			_configDir = configDir;
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			LastUsedTime = DateTime.Now;
			Tokenizer = new RegexTokenizer(new IntegerSpanFactory(), @"[\p{P}]|(\w+([.,\-’']\w+)*)");
			Detokenizer = new SimpleStringDetokenizer(GetDetokenizeOperation);
		}

		private static DetokenizeOperation GetDetokenizeOperation(string token)
		{
			if (token.Any(char.IsPunctuation))
			{
				if (MergeRightTokens.Contains(token))
					return DetokenizeOperation.MergeRight;
				if (MergeRightFirstLeftSecondTokens.Contains(token))
					return DetokenizeOperation.MergeRightFirstLeftSecond;
				return DetokenizeOperation.MergeLeft;
			}

			return DetokenizeOperation.NoOperation;
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public bool IsLoaded { get; private set; }
		public DateTime LastUsedTime { get; set; }
		public HybridTranslationEngine Engine { get; set; }
		public ITokenizer<string, int> Tokenizer { get; }
		public IDetokenizer<string, string> Detokenizer { get; }

		public void MarkUpdated()
		{
			_isUpdated = true;
		}

		public void Save()
		{
			if (_isUpdated)
			{
				_smtModel.Save();
				_isUpdated = false;
			}
		}

		public void Load()
		{
			if (IsLoaded)
				return;

			string smtConfigFileName = Path.Combine(_configDir, "smt.cfg");
			_smtModel = new ThotSmtModel(smtConfigFileName);

			string hcSrcConfigFileName = Path.Combine(_configDir, string.Format("{0}-hc.xml", SourceLanguageTag));
			string hcTrgConfigFileName = Path.Combine(_configDir, string.Format("{0}-hc.xml", TargetLanguageTag));
			TransferEngine transferEngine = null;
			if (File.Exists(hcSrcConfigFileName) && File.Exists(hcTrgConfigFileName))
			{
				var spanFactory = new ShapeSpanFactory();
				var hcTraceManager = new TraceManager();

				Language srcLang = XmlLanguageLoader.Load(hcSrcConfigFileName);
				var srcMorpher = new Morpher(spanFactory, hcTraceManager, srcLang);

				Language trgLang = XmlLanguageLoader.Load(hcTrgConfigFileName);
				var trgMorpher = new Morpher(spanFactory, hcTraceManager, trgLang);

				transferEngine = new TransferEngine(srcMorpher, new SimpleTransferer(new GlossMorphemeMapper(trgMorpher)), trgMorpher);
			}

			Engine = new HybridTranslationEngine(_smtModel.CreateEngine(), transferEngine);
			IsLoaded = true;
		}

		public void Unload()
		{
			if (!IsLoaded)
				return;

			Engine.Dispose();
			Engine = null;
			Save();
			_smtModel.Dispose();
			_smtModel = null;
			IsLoaded = false;
		}
	}
}
