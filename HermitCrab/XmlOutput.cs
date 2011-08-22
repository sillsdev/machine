using System.Collections.Generic;
using System.Xml;
using SIL.APRE;
using SIL.APRE.Patterns;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a simple XML representation of HC objects. It writes out the results to the provided
	/// XML writer.
	/// </summary>
	public class XmlOutput : IOutput
	{
		private readonly XmlWriter _xmlWriter;

		public XmlOutput(XmlWriter writer)
		{
			_xmlWriter = writer;
		}

		public XmlWriter XmlWriter
		{
			get
			{
				return _xmlWriter;
			}
		}

		public virtual void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs)
		{
			_xmlWriter.WriteStartElement("MorphAndLookupWord");
			_xmlWriter.WriteElementString("Input", word);
			try
			{
				WordAnalysisTrace trace;
				ICollection<WordSynthesis> results = morpher.MorphAndLookupWord(word, out trace);
				_xmlWriter.WriteStartElement("Output");
				foreach (WordSynthesis ws in results)
					Write(ws, prettyPrint);
				_xmlWriter.WriteEndElement();

				if (morpher.IsTracing)
				{
					Write(trace, prettyPrint, printTraceInputs);
				}
			}
			catch (MorphException me)
			{
				Write(me);
			}
			_xmlWriter.WriteEndElement();
		}

		public virtual void Write(WordSynthesis ws, bool prettyPrint)
		{
			_xmlWriter.WriteStartElement("Result");
			Write("Root", ws.Root);
			_xmlWriter.WriteElementString("POS", ws.PartOfSpeech.Description);

			_xmlWriter.WriteStartElement("Morphs");
			foreach (Morph morph in ws.Morphs)
				Write("Allomorph", morph.Allomorph);
			_xmlWriter.WriteEndElement();

			_xmlWriter.WriteElementString("MPRFeatures", ws.MPRFeatures.ToString());
			_xmlWriter.WriteElementString("HeadFeatures", ws.HeadFeatures.ToString());
			_xmlWriter.WriteElementString("FootFeatures", ws.FootFeatures.ToString());

			_xmlWriter.WriteEndElement();
		}

		public virtual void Write(Trace trace, bool prettyPrint, bool printTraceInputs)
		{
			_xmlWriter.WriteStartElement("Trace");
			Write(trace, printTraceInputs);
			_xmlWriter.WriteEndElement();
		}

		protected virtual void Write(Trace trace, bool printTraceInputs)
		{
			switch (trace.Type)
			{
				case Trace.TraceType.WordAnalysis:
					var waTrace = (WordAnalysisTrace) trace;
					_xmlWriter.WriteStartElement(waTrace.GetType().Name);
					_xmlWriter.WriteElementString("InputWord", waTrace.InputWord);
					break;

				case Trace.TraceType.StratumAnalysis:
					var saTrace = (StratumAnalysisTrace) trace;
					_xmlWriter.WriteStartElement(saTrace.GetType().Name + (saTrace.IsInput ? "In" : "Out"));
					Write("Stratum", saTrace.Stratum);
					Write(saTrace.IsInput ? "Input" : "Output", saTrace.Analysis);
					break;

				case Trace.TraceType.StratumSynthesis:
					var ssTrace = (StratumSynthesisTrace) trace;
					_xmlWriter.WriteStartElement(ssTrace.GetType().Name + (ssTrace.IsInput ? "In" : "Out"));
					Write("Stratum", ssTrace.Stratum);
					Write(ssTrace.IsInput ? "Input" : "Output", ssTrace.Synthesis);
					break;

				case Trace.TraceType.LexicalLookup:
					var llTrace = (LexLookupTrace) trace;
					_xmlWriter.WriteStartElement(llTrace.GetType().Name);
					_xmlWriter.WriteElementString("Stratum", llTrace.Stratum.Description);
					_xmlWriter.WriteElementString("Shape", llTrace.Stratum.CharacterDefinitionTable.ToRegexString(llTrace.Shape,
						ModeType.Analysis, true));
					break;

				case Trace.TraceType.WordSynthesis:
					var wsTrace = (WordSynthesisTrace) trace;
					_xmlWriter.WriteStartElement(wsTrace.GetType().Name);
					Write("RootAllomorph", wsTrace.RootAllomorph);
					_xmlWriter.WriteStartElement("MorphologicalRules");
					foreach (MorphologicalRule rule in wsTrace.MorphologicalRules)
						Write("MorphologicalRule", rule);
					_xmlWriter.WriteEndElement(); // MorphologicalRules
					_xmlWriter.WriteElementString("RealizationalFeatures", wsTrace.RealizationalFeatures.ToString());
					break;

				case Trace.TraceType.PhonologicalRuleAnalysis:
					var paTrace = (PhonologicalRuleAnalysisTrace) trace;
					_xmlWriter.WriteStartElement(paTrace.GetType().Name);
					Write("PhonologicalRule", paTrace.Rule);
					if (printTraceInputs)
						Write("Input", paTrace.Input);
					Write("Output", paTrace.Output);
					break;

				case Trace.TraceType.PhonologicalRuleSynthesis:
					var psTrace = (PhonologicalRuleSynthesisTrace) trace;
					_xmlWriter.WriteStartElement(psTrace.GetType().Name);
					Write("PhonologicalRule", psTrace.Rule);
					if (printTraceInputs)
						Write("Input", psTrace.Input);
					Write("Output", psTrace.Output);
					break;

				case Trace.TraceType.TemplateAnalysis:
					var taTrace = (TemplateAnalysisTrace) trace;
					_xmlWriter.WriteStartElement(taTrace.GetType().Name + (taTrace.IsInput ? "In" : "Out"));
					Write("AffixTemplate", taTrace.Template);
					Write(taTrace.IsInput ? "Input" : "Output", taTrace.Analysis);
					break;

				case Trace.TraceType.TemplateSynthesis:
					var tsTrace = (TemplateSynthesisTrace) trace;
					_xmlWriter.WriteStartElement(tsTrace.GetType().Name + (tsTrace.IsInput ? "In" : "Out"));
					Write("AffixTemplate", tsTrace.Template);
					Write(tsTrace.IsInput ? "Input" : "Output", tsTrace.Synthesis);
					break;

				case Trace.TraceType.MorphologicalRuleAnalysis:
					var maTrace = (MorphologicalRuleAnalysisTrace) trace;
					_xmlWriter.WriteStartElement(maTrace.GetType().Name);
					Write("MorphologicalRule", maTrace.Rule);
					if (maTrace.RuleAllomorph != null)
						Write("RuleAllomorph", maTrace.RuleAllomorph);
					if (printTraceInputs)
						Write("Input", maTrace.Input);
					Write("Output", maTrace.Output);
					break;

				case Trace.TraceType.MorphologicalRuleSynthesis:
					var msTrace = (MorphologicalRuleSynthesisTrace) trace;
					_xmlWriter.WriteStartElement(msTrace.GetType().Name);
					Write("MorphologicalRule", msTrace.Rule);
					if (msTrace.RuleAllomorph != null)
						Write("RuleAllomorph", msTrace.RuleAllomorph);
					if (printTraceInputs)
						Write("Input", msTrace.Input);
					Write("Output", msTrace.Output);
					break;

				case Trace.TraceType.Blocking:
					var bTrace = (BlockingTrace) trace;
					_xmlWriter.WriteStartElement(bTrace.GetType().Name);
					Write("BlockingEntry", bTrace.BlockingEntry);
					break;

				case Trace.TraceType.ReportSuccess:
					var rsTrace = (ReportSuccessTrace) trace;
					_xmlWriter.WriteStartElement(rsTrace.GetType().Name);
					Write("Result", rsTrace.Output);
					break;
			}
			foreach (Trace child in trace.Children)
				Write(child, printTraceInputs);
			_xmlWriter.WriteEndElement();
		}

		protected virtual void Write(string localName, Morpheme morpheme)
		{
			_xmlWriter.WriteStartElement(localName);
			_xmlWriter.WriteAttributeString("id", morpheme.ID);
			_xmlWriter.WriteElementString("Description", morpheme.Description);
			if (morpheme.Gloss != null)
				_xmlWriter.WriteElementString("Gloss", morpheme.Gloss.Description);
			_xmlWriter.WriteEndElement();
		}

		protected virtual void Write(string localName, WordAnalysis wa)
		{
			_xmlWriter.WriteElementString(localName, wa == null ? HCStrings.kstidTraceNoOutput
				: wa.Stratum.CharacterDefinitionTable.ToRegexString(wa.Shape, ModeType.Analysis, true));
		}

		protected virtual void Write(string localName, WordSynthesis ws)
		{
			_xmlWriter.WriteElementString(localName, ws == null ? HCStrings.kstidTraceNoOutput
				: ws.Stratum.CharacterDefinitionTable.ToString(ws.Shape, ModeType.Synthesis, true));
		}

		protected virtual void Write(string localName, Allomorph allo)
		{
			_xmlWriter.WriteStartElement(localName);
			_xmlWriter.WriteAttributeString("id", allo.ID);
			_xmlWriter.WriteElementString("Description", allo.Description);
			Write("Morpheme", allo.Morpheme);
			_xmlWriter.WriteStartElement("Properties");
			foreach (KeyValuePair<string, string> prop in allo.Properties)
			{
				_xmlWriter.WriteStartElement("Property");
				_xmlWriter.WriteElementString("Key", prop.Key);
				_xmlWriter.WriteElementString("Value", prop.Value);
				_xmlWriter.WriteEndElement();
			}
			_xmlWriter.WriteEndElement();
			_xmlWriter.WriteEndElement();
		}

		protected virtual void Write(string localName, HCObject obj)
		{
			_xmlWriter.WriteStartElement(localName);
			_xmlWriter.WriteAttributeString("id", obj.ID);
			_xmlWriter.WriteElementString("Description", obj.Description);
			_xmlWriter.WriteEndElement();
		}

		public virtual void Write(LoadException le)
		{
			_xmlWriter.WriteElementString("LoadError", le.Message);
		}

		public virtual void Write(MorphException me)
		{
			_xmlWriter.WriteElementString("MorphError", me.Message);
		}

		public virtual void Flush()
		{
			_xmlWriter.Flush();
		}

		public virtual void Close()
		{
			_xmlWriter.Close();
		}
	}
}
