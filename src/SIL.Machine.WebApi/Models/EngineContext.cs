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

		private ThotSmtModel _smtModel;

		public EngineContext(string sourceLanguageTag, string targetLanguageTag)
		{
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
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

		public void Load(string rootDir)
		{
			if (IsLoaded)
				return;

			string configDir = Path.Combine(rootDir, string.Format("{0}_{1}", SourceLanguageTag, TargetLanguageTag));
			string smtConfigFileName = Path.Combine(configDir, "smt.cfg");
			_smtModel = new ThotSmtModel(smtConfigFileName);

			string hcSrcConfigFileName = Path.Combine(configDir, string.Format("{0}-hc.xml", SourceLanguageTag));
			string hcTrgConfigFileName = Path.Combine(configDir, string.Format("{0}-hc.xml", TargetLanguageTag));
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
			_smtModel.Save();
			_smtModel.Dispose();
			_smtModel = null;
			IsLoaded = false;
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public bool IsLoaded { get; private set; }
		public DateTime LastUsedTime { get; set; }
		public HybridTranslationEngine Engine { get; set; }
		public ITokenizer<string, int> Tokenizer { get; }
		public IDetokenizer<string, string> Detokenizer { get; }
	}
}
