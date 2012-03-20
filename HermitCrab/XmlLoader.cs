using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Linq;

using SIL.Collections;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents the loader for HC.NET's XML input format.
	/// </summary>
	public class XmlLoader
	{
		public Language Load(string configPath)
		{
			return Load(configPath, false);
		}

		public Language Load(string configPath, bool quitOnError)
		{
			return Load(configPath, quitOnError, null);
		}

		public Language Load(string configPath, bool quitOnError, XmlResolver resolver)
		{
			var loader = new XmlLoader(configPath, quitOnError, resolver);
			return loader.Load();
		}

		private static RuleCascadeOrder GetMRuleOrder(string ruleOrderStr)
		{
			switch (ruleOrderStr)
			{
				case "linear":
					return RuleCascadeOrder.Linear;

				case "unordered":
					return RuleCascadeOrder.Combination;
			}

			return RuleCascadeOrder.Combination;
		}

		private static ApplicationMode GetMultAppOrder(string multAppOrderStr)
		{
			switch (multAppOrderStr)
			{
				case "simultaneous":
					return ApplicationMode.Simultaneous;

				case "rightToLeftIterative":
				case "leftToRightIterative":
					return ApplicationMode.Iterative;
			}

			return ApplicationMode.Iterative;
		}

		private static MprFeatureGroupMatchType GetGroupMatchType(string matchTypeStr)
		{
			switch (matchTypeStr)
			{
				case "all":
					return MprFeatureGroupMatchType.All;

				case "any":
					return MprFeatureGroupMatchType.Any;
			}
			return MprFeatureGroupMatchType.Any;
		}

		private static MprFeatureGroupOutput GetGroupOutput(string outputTypeStr)
		{
			switch (outputTypeStr)
			{
				case "overwrite":
					return MprFeatureGroupOutput.Overwrite;

				case "append":
					return MprFeatureGroupOutput.Append;
			}
			return MprFeatureGroupOutput.Overwrite;
		}

		private static ReduplicationHint GetReduplicationHint(string redupMorphTypeStr)
		{
			switch (redupMorphTypeStr)
			{
				case "prefix":
					return ReduplicationHint.Prefix;

				case "suffix":
					return ReduplicationHint.Suffix;

				case "implicit":
					return ReduplicationHint.Implicit;
			}
			return ReduplicationHint.Implicit;
		}

		private static MorphCoOccurrenceAdjacency GetAdjacencyType(string adjacencyTypeStr)
		{
			switch (adjacencyTypeStr)
			{
				case "anywhere":
					return MorphCoOccurrenceAdjacency.Anywhere;

				case "somewhereToLeft":
					return MorphCoOccurrenceAdjacency.SomewhereToLeft;

				case "somewhereToRight":
					return MorphCoOccurrenceAdjacency.SomewhereToRight;

				case "adjacentToLeft":
					return MorphCoOccurrenceAdjacency.AdjacentToLeft;

				case "adjacentToRight":
					return MorphCoOccurrenceAdjacency.AdjacentToRight;
			}
			return MorphCoOccurrenceAdjacency.Anywhere;
		}

		private Language _language;

		private readonly SymbolicFeature _posFeature;
		private readonly ComplexFeature _headFeature;
		private readonly ComplexFeature _footFeature;

		private readonly string _configPath;
		private readonly bool _quitOnError;
		private readonly Dictionary<string, string> _repIds;
		private readonly ShapeSpanFactory _spanFactory;
		private readonly XmlResolver _resolver;

		private readonly IDBearerSet<SymbolTable> _tables; 
		private readonly IDBearerSet<MprFeature> _mprFeatures;
		private readonly IDBearerSet<MprFeatureGroup> _mprFeatureGroups;
		private readonly Dictionary<string, FeatureStruct> _natClasses;

		private XmlLoader(string configPath, bool quitOnError, XmlResolver resolver)
		{
			_configPath = configPath;
			_quitOnError = quitOnError;
			_repIds = new Dictionary<string, string>();
			_spanFactory = new ShapeSpanFactory();
			_resolver = resolver;

			_posFeature = new SymbolicFeature(Guid.NewGuid().ToString()) { Description = "POS" };
			_headFeature = new ComplexFeature(Guid.NewGuid().ToString()) { Description = "Head" };
			_footFeature = new ComplexFeature(Guid.NewGuid().ToString()) { Description = "Foot" };

			_tables = new IDBearerSet<SymbolTable>();
			_mprFeatures = new IDBearerSet<MprFeature>();
			_mprFeatureGroups = new IDBearerSet<MprFeatureGroup>();
			_natClasses = new Dictionary<string, FeatureStruct>();
		}

		private Language Load()
		{
			var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
			if (Type.GetType("Mono.Runtime") == null)
			{
				settings.ValidationType = ValidationType.DTD;
				settings.ValidationEventHandler += ValidationEventHandler;
			}
			else
			{
				// Mono's dtd processing seems to have bugs. Workaround	don't do DTD validation.
				settings.ValidationType = ValidationType.None;
			}
			
			if (_resolver != null)
				settings.XmlResolver = _resolver;

			XmlReader reader = XmlReader.Create(_configPath, settings);
			XDocument doc;
			try
			{
				doc = XDocument.Load(reader);
			}
			catch (XmlException xe)
			{
				throw new LoadException(LoadError.ParseError, string.Format("Unable to parser input file: {0}.", _configPath), xe);
			}
			finally
			{
				reader.Close();
			}

			LoadLanguage(doc.Elements("Language").Single(IsActive));
			return _language;
		}

		private static void ValidationEventHandler(object sender, ValidationEventArgs e)
		{
			throw new LoadException(LoadError.InvalidFormat, e.Message + " Line: " + e.Exception.LineNumber + ", Pos: " + e.Exception.LinePosition);
		}

		private static bool IsActive(XElement elem)
		{
			return (string) elem.Attribute("isActive") == "yes";
		}

		private void LoadLanguage(XElement langElem)
		{
			var id = (string) langElem.Attribute("id");
			_language = new Language(id) { Description = (string) langElem.Element("Name") };

			foreach (XElement posElem in langElem.Elements("PartsOfSpeech").Elements("PartOfSpeech"))
				_posFeature.PossibleSymbols.Add(new FeatureSymbol((string)posElem.Attribute("id")) { Description = (string) posElem });
			_language.SyntacticFeatureSystem.Add(_posFeature);

			_mprFeatures.UnionWith(from elem in langElem.Elements("MorphologicalPhonologicalRuleFeatures").Elements("MorphologicalPhonologicalRuleFeature")
								   where IsActive(elem)
								   select new MprFeature((string) elem.Attribute("id")) { Description = (string) elem });

			foreach (XElement mprFeatGroupElem in langElem.Elements("MorphologicalPhonologicalRuleFeatures").Elements("MorphologicalPhonologicalRuleFeatureGroup").Where(IsActive))
				LoadMprFeatGroup(mprFeatGroupElem);

			LoadFeatureSystem(langElem.Elements("PhonologicalFeatureSystem").Single(IsActive), _language.PhoneticFeatureSystem);

			_language.SyntacticFeatureSystem.Add(_headFeature);
			XElement headFeatsElem = langElem.Element("HeadFeatures");
			if (headFeatsElem != null)
				LoadFeatureSystem(headFeatsElem, _language.SyntacticFeatureSystem);
			_language.SyntacticFeatureSystem.Add(_footFeature);
			XElement footFeatsElem = langElem.Element("FootFeatures");
			if (footFeatsElem != null)
				LoadFeatureSystem(footFeatsElem, _language.SyntacticFeatureSystem);

			foreach (XElement charDefTableElem in langElem.Elements("CharacterDefinitionTable").Where(IsActive))
				LoadSymbolTable(charDefTableElem);

			foreach (XElement natClassElem in langElem.Elements("NaturalClasses").Elements("FeatureNaturalClass").Where(IsActive))
				_natClasses[(string) natClassElem.Attribute("id")] = LoadPhoneticFeatureStruct(natClassElem);

			foreach (XElement natClassElem in langElem.Elements("NaturalClasses").Elements("SegmentNaturalClass").Where(IsActive))
				LoadSegNatClass(natClassElem);

			var mrules = new IDBearerSet<IMorphologicalRule>();
			foreach (XElement mruleElem in langElem.Elements("Rules").Elements().Where(IsActive))
			{
				try
				{
					switch (mruleElem.Name.LocalName)
					{
						case "MorphologicalRule":
							AffixProcessRule aprule = LoadAffixProcessRule(mruleElem);
							if (aprule.Allomorphs.Count > 0)
								mrules.Add(aprule);
							break;

						case "RealizationalRule":
							RealizationalAffixProcessRule realRule = LoadRealizationalRule(mruleElem);
							if (realRule.Allomorphs.Count > 0)
								mrules.Add(realRule);
							break;

						case "CompoundingRule":
							LoadCompoundingRule(mruleElem);
							break;
					}
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			IDBearerSet<MorphologicalRule> templateRules = new IDBearerSet<MorphologicalRule>();
			XmlNodeList tempList = langElem.SelectNodes("Strata/AffixTemplate[@isActive='yes']");
			foreach (XmlNode tempNode in tempList)
				LoadAffixTemplate(tempNode as XmlElement, templateRules);

			XmlNodeList stratumList = langElem.SelectNodes("Strata/Stratum[@isActive='yes']");
			XmlElement surfaceElem = null;
			foreach (XmlNode stratumNode in stratumList)
			{
				XmlElement stratumElem = stratumNode as XmlElement;
				if (stratumElem.GetAttribute("id") == Stratum.SurfaceStratumID)
					surfaceElem = stratumElem;
				else
					LoadStratum(stratumElem);
			}
			if (surfaceElem == null)
				throw CreateUndefinedObjectException(HCStrings.kstidNoSurfaceStratum, Stratum.SurfaceStratumID);
			LoadStratum(surfaceElem);

			if (mrulesNodeList != null)
			{
				foreach (XmlNode mruleNode in mrulesNodeList)
				{
					XmlElement mruleElem = mruleNode as XmlElement;
					string ruleId = mruleElem.GetAttribute("id");
					if (!templateRules.Contains(ruleId))
					{
						MorphologicalRule mrule = _curMorpher.GetMorphologicalRule(ruleId);
						if (mrule != null)
						{
							Stratum stratum = _curMorpher.GetStratum(mruleElem.GetAttribute("stratum"));
							stratum.AddMorphologicalRule(mrule);
						}
					}
				}
			}

			XmlNodeList familyList = langElem.SelectNodes("Lexicon/Families/Family[@isActive='yes']");
			foreach (XmlNode familyNode in familyList)
			{
				XmlElement familyElem = familyNode as XmlElement;
				LexFamily family = new LexFamily(familyElem.GetAttribute("id"), familyElem.InnerText, _curMorpher);
				_curMorpher.Lexicon.AddFamily(family);
			}

			XmlNodeList entryList = langElem.SelectNodes("Lexicon/LexicalEntry[@isActive='yes']");
			foreach (XmlNode entryNode in entryList)
			{
				try
				{
					LoadLexEntry(entryNode as XmlElement);
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			// co-occurrence rules cannot be loaded until all of the morphemes and their allomorphs have been loaded
			XmlNodeList morphemeList = langElem.SelectNodes("Lexicon/LexicalEntry[@isActive='yes'] | Rules/*[@isActive='yes']");
			foreach (XmlNode morphemeNode in morphemeList)
			{
				XmlElement morphemeElem = morphemeNode as XmlElement;
				string morphemeId = morphemeElem.GetAttribute("id");
				Morpheme morpheme = _curMorpher.GetMorpheme(morphemeId);
				if (morpheme != null)
				{
					try
					{
						morpheme.RequiredMorphemeCoOccurrences = LoadMorphCoOccurs(morphemeElem.SelectSingleNode("RequiredMorphemeCoOccurrences"));
					}
					catch (LoadException le)
					{
						if (m_quitOnError)
							throw le;
					}
					try
					{
						morpheme.ExcludedMorphemeCoOccurrences = LoadMorphCoOccurs(morphemeElem.SelectSingleNode("ExcludedMorphemeCoOccurrences"));
					}
					catch (LoadException le)
					{
						if (m_quitOnError)
							throw le;
					}
				}

				XmlNodeList allomorphList = morphemeNode.SelectNodes("Allomorphs/Allomorph[@isActive='yes'] | MorphologicalSubrules/MorphologicalSubruleStructure[@isActive='yes']");
				foreach (XmlNode alloNode in allomorphList)
				{
					XmlElement alloElem = alloNode as XmlElement;
					string alloId = alloElem.GetAttribute("id");
					Allomorph allomorph = _curMorpher.GetAllomorph(alloId);
					if (allomorph != null)
					{
						try
						{
							allomorph.RequiredAllomorphCoOccurrences = LoadAlloCoOccurs(alloElem.SelectSingleNode("RequiredAllomorphCoOccurrences"));
						}
						catch (LoadException)
						{
							if (_quitOnError)
								throw;
						}
						try
						{
							allomorph.ExcludedAllomorphCoOccurrences = LoadAlloCoOccurs(alloElem.SelectSingleNode("ExcludedAllomorphCoOccurrences"));
						}
						catch (LoadException)
						{
							if (_quitOnError)
								throw;
						}
					}
				}
			}

			XmlNodeList prules = langElem.SelectNodes("PhonologicalRules/*[@isActive='yes']");
			foreach (XmlNode pruleNode in prules)
			{
				XmlElement pruleElem = pruleNode as XmlElement;
				try
				{
					switch (pruleElem.Name)
					{
						case "MetathesisRule":
							LoadMetathesisRule(pruleElem);
							break;

						case "PhonologicalRule":
							LoadPRule(pruleElem);
							break;
					}
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}
		}

		private void LoadMprFeatGroup(XElement mprFeatGroupElem)
		{
			var group = new MprFeatureGroup((string) mprFeatGroupElem.Attribute("id")) { Description = (string) mprFeatGroupElem.Element("Name") };
			group.MatchType = GetGroupMatchType((string) mprFeatGroupElem.Attribute("matchType"));
			group.Output = GetGroupOutput((string) mprFeatGroupElem.Attribute("outputType"));
			var mprFeatIdsStr = (string) mprFeatGroupElem.Attribute("features");
			foreach (MprFeature mprFeat in LoadMprFeatures(mprFeatIdsStr))
				group.Add(mprFeat);
			_mprFeatureGroups.Add(group);
		}

		void LoadStratum(XmlElement stratumNode)
		{
			string id = stratumNode.GetAttribute("id");
			string name = stratumNode.SelectSingleNode("Name").InnerText;
			Stratum stratum = new Stratum(id, name, _curMorpher);
			stratum.SymbolTable = GetCharDefTable(stratumNode.GetAttribute("characterDefinitionTable"));
			stratum.IsCyclic = stratumNode.GetAttribute("cyclicity") == "cyclic";
			stratum.PhonologicalRuleOrder = GetPRuleOrder(stratumNode.GetAttribute("phonologicalRuleOrder"));
			stratum.MorphologicalRuleOrder = GetMRuleOrder(stratumNode.GetAttribute("morphologicalRuleOrder"));

			string tempIdsStr = stratumNode.GetAttribute("affixTemplates");
			if (!string.IsNullOrEmpty(tempIdsStr))
			{
				string[] tempIds = tempIdsStr.Split(' ');
				foreach (string tempId in tempIds)
				{
					AffixTemplate template = _curMorpher.GetAffixTemplate(tempId);
					if (template == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownTemplate, tempId), tempId);
					stratum.AddAffixTemplate(template);
				}
			}

			_curMorpher.AddStratum(stratum);
		}

		void LoadLexEntry(XmlElement entryNode)
		{
			string id = entryNode.GetAttribute("id");
			LexEntry entry = new LexEntry(id, id, _curMorpher);

			string posId = entryNode.GetAttribute("partOfSpeech");
			PartOfSpeech pos = _curMorpher.GetPartOfSpeech(posId);
			if (pos == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownPOS, posId), posId);
			entry.PartOfSpeech = pos;
			XmlElement glossElem = entryNode.SelectSingleNode("Gloss") as XmlElement;
			entry.Gloss = new Gloss(glossElem.GetAttribute("id"), glossElem.InnerText, _curMorpher);

			entry.MprFeatures = LoadMprFeatures(entryNode.GetAttribute("ruleFeatures"));

			entry.HeadFeatures = LoadSynFeats(entryNode.SelectSingleNode("HeadFeatures"),
				_curMorpher.HeadFeatureSystem);

			entry.FootFeatures = LoadSynFeats(entryNode.SelectSingleNode("FootFeatures"),
				_curMorpher.FootFeatureSystem);

			Stratum stratum = GetStratum(entryNode.GetAttribute("stratum"));

			string familyId = entryNode.GetAttribute("family");
			if (!string.IsNullOrEmpty(familyId))
			{
				LexFamily family = _curMorpher.Lexicon.GetFamily(familyId);
				if (family == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFamily, familyId), familyId);
				family.AddEntry(entry);
			}

			XmlNodeList alloNodes = entryNode.SelectNodes("Allomorphs/Allomorph[@isActive='yes']");
			foreach (XmlNode alloNode in alloNodes)
			{
				try
				{
					LoadAllomorph(alloNode as XmlElement, entry, stratum);
				}
				catch (LoadException le)
				{
					if (m_quitOnError)
						throw le;
				}
			}

			if (entry.AllomorphCount > 0)
			{
				stratum.AddEntry(entry);
				_curMorpher.Lexicon.AddEntry(entry);
			}
		}

		void LoadAllomorph(XmlElement alloNode, LexEntry entry, Stratum stratum)
		{
			string alloId = alloNode.GetAttribute("id");
			string shapeStr = alloNode.SelectSingleNode("PhoneticShape").InnerText;
			Shape shape = stratum.SymbolTable.ToShape(shapeStr, ModeType.Synthesis);
			if (shape == null)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.InvalidEntryShape, this,
					string.Format(HCStrings.kstidInvalidLexEntryShape, shapeStr, entry.ID, stratum.SymbolTable.ID));
				le.Data["shape"] = shapeStr;
				le.Data["charDefTable"] = stratum.SymbolTable.ID;
				le.Data["entry"] = entry.ID;
				throw le;
			}
			LexEntry.RootAllomorph allomorph = new LexEntry.RootAllomorph(alloId, shapeStr, _curMorpher, shape);
			allomorph.RequiredEnvironments = LoadAllomorphEnvironments(alloNode.SelectSingleNode("RequiredEnvironments"));
			allomorph.ExcludedEnvironments = LoadAllomorphEnvironments(alloNode.SelectSingleNode("ExcludedEnvironments"));
			allomorph.Properties = LoadProperties(alloNode.SelectSingleNode("Properties"));
			entry.AddAllomorph(allomorph);

			_curMorpher.AddAllomorph(allomorph);
		}

		private void LoadAllomorphEnvironments(XElement envsElem, ICollection<AllomorphEnvironment> envs)
		{
			if (envsElem == null)
				return;

			foreach (XElement envElem in envsElem.Elements("Environment"))
				envs.Add(LoadAllomorphEnvironment(envElem));
		}

		private AllomorphEnvironment LoadAllomorphEnvironment(XElement envElem)
		{
			var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();
			Pattern<Word, ShapeNode> leftEnv = LoadPattern(envElem.Elements("LeftEnvironment").Elements("PhoneticTemplate").Single(), variables);
			Pattern<Word, ShapeNode> rightEnv = LoadPattern(envElem.Elements("RightEnvironment").Elements("PhoneticTemplate").Single(), variables);
			return new AllomorphEnvironment(_spanFactory, leftEnv, rightEnv);
		}

		IEnumerable<MorphCoOccurrence> LoadMorphCoOccurs(XmlNode coOccursNode)
		{
			if (coOccursNode == null)
				return null;

			List<MorphCoOccurrence> coOccurs = new List<MorphCoOccurrence>();
			XmlNodeList coOccurList = coOccursNode.SelectNodes("MorphemeCoOccurrence");
			foreach (XmlNode coOccurNode in coOccurList)
				coOccurs.Add(LoadMorphCoOccur(coOccurNode as XmlElement));
			return coOccurs;
		}

		MorphCoOccurrence LoadMorphCoOccur(XmlElement coOccurNode)
		{
			MorphCoOccurrence.AdjacencyType adjacency = GetAdjacencyType(coOccurNode.GetAttribute("adjacency"));
			string[] morphemeIds = coOccurNode.GetAttribute("morphemes").Split(' ');
			IDBearerSet<HCObject> morphemes = new IDBearerSet<HCObject>();
			foreach (string morphemeId in morphemeIds)
			{
				Morpheme morpheme = _curMorpher.GetMorpheme(morphemeId);
				if (morpheme == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownMorpheme, morphemeId), morphemeId);
				morphemes.Add(morpheme);
			}
			return new MorphCoOccurrence(morphemes, MorphCoOccurrence.ObjectType.Morpheme, adjacency);
		}

		IEnumerable<MorphCoOccurrence> LoadAlloCoOccurs(XmlNode coOccursNode)
		{
			if (coOccursNode == null)
				return null;

			List<MorphCoOccurrence> coOccurs = new List<MorphCoOccurrence>();
			XmlNodeList coOccurList = coOccursNode.SelectNodes("AllomorphCoOccurrence");
			foreach (XmlNode coOccurNode in coOccurList)
				coOccurs.Add(LoadAlloCoOccur(coOccurNode as XmlElement));
			return coOccurs;
		}

		MorphCoOccurrence LoadAlloCoOccur(XmlElement coOccurNode)
		{
			MorphCoOccurrence.AdjacencyType adjacency = GetAdjacencyType(coOccurNode.GetAttribute("adjacency"));
			string[] allomorphIds = coOccurNode.GetAttribute("allomorphs").Split(' ');
			IDBearerSet<HCObject> allomorphs = new IDBearerSet<HCObject>();
			foreach (string allomorphId in allomorphIds)
			{
				Allomorph allomorph = _curMorpher.GetAllomorph(allomorphId);
				if (allomorph == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownAllo, allomorphId), allomorphId);
				allomorphs.Add(allomorph);
			}
			return new MorphCoOccurrence(allomorphs, MorphCoOccurrence.ObjectType.Allomorph, adjacency);
		}

		private void LoadProperties(XElement propsElem, IDictionary<string, string> props)
		{
			if (propsElem == null)
				return;

			foreach (XElement propElem in propsElem.Elements("Property"))
				props[(string) propElem.Attribute("name")] = (string) propElem;
		}

		private FeatureStruct LoadSyntacticFeatureStruct(XElement elem)
		{
			var fs = new FeatureStruct();
			foreach (XElement featValElem in elem.Elements("FeatureValueList").Where(IsActive))
			{
				Feature feature = GetFeature(_language.SyntacticFeatureSystem, (string) featValElem.Attribute("feature"));
				var valueIDsStr = (string) featValElem.Attribute("values");
				if (!string.IsNullOrEmpty(valueIDsStr))
				{
					var sf = (SymbolicFeature) feature;
					fs.AddValue(sf, valueIDsStr.Split(' ').Select(id => GetFeatureSymbol(sf, id)));
				}
				else
				{
					var cf = (ComplexFeature) feature;
					fs.AddValue(cf, LoadSyntacticFeatureStruct(featValElem));
				}
			}

			return fs;
		}

		private void LoadFeatureSystem(XElement featSysElem, FeatureSystem featSys)
		{
			foreach (XElement featDefElem in featSysElem.Elements("FeatureDefinition").Where(IsActive))
				featSys.Add(LoadFeature(featDefElem));
		}

		private Feature LoadFeature(XElement featDefElem)
		{
			XElement featElem = featDefElem.Element("Feature");
			Debug.Assert(featElem != null);
			var id = (string) featElem.Attribute("id");
			var name = (string) featElem;

			XElement valueListElem = featDefElem.Element("ValueList");
			if (valueListElem != null)
			{
				var feature = new SymbolicFeature(id) { Description = name };
				foreach (XElement valueElem in valueListElem.Elements("Value"))
				{
					var symbol = new FeatureSymbol((string) valueElem.Attribute("id")) { Description = (string) valueElem };
					feature.PossibleSymbols.Add(symbol);
				}
				var defValId = (string) featElem.Attribute("defaultValue");
				if (!string.IsNullOrEmpty(defValId))
					feature.DefaultValue = new SymbolicFeatureValue(feature.PossibleSymbols[defValId]);
				return feature;
			}

			return new ComplexFeature(id) { Description = name };
		}

		private void LoadSymbolTable(XElement charDefTableElem)
		{
			var table = new SymbolTable(_spanFactory, (string) charDefTableElem.Attribute("id")) { Description = (string) charDefTableElem.Element("Name") };
			foreach (XElement segDefElem in charDefTableElem.Elements("SegmentDefinitions").Elements("SegmentDefinition").Where(IsActive))
			{
				XElement repElem = segDefElem.Element("Representation");
				Debug.Assert(repElem != null);
				var strRep = (string) repElem;
				FeatureStruct fs = _language.PhoneticFeatureSystem.Count > 0 ? LoadPhoneticFeatureStruct(segDefElem)
					: FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Feature(HCFeatureSystem.StrRep).EqualTo(strRep).Value;
				table.Add(strRep, fs);
				_repIds[(string) repElem.Attribute("id")] = strRep;
			}

			foreach (XElement bdryDefElem in charDefTableElem.Elements("BoundaryDefinitions").Elements("BoundarySymbol"))
			{
				var strRep = (string) bdryDefElem;
				table.Add(strRep, FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo(strRep).Value);
				_repIds[(string) bdryDefElem.Attribute("id")] = strRep;
			}

			_tables.Add(table);
		}

		private void LoadSegNatClass(XElement natClassElem)
		{
			var id = (string) natClassElem.Attribute("id");
			FeatureStruct fs = null;
			foreach (XElement segElem in natClassElem.Elements("Segment"))
			{
				SymbolTable table = GetTable((string) segElem.Attribute("characterTable"));
				string strRep = GetStrRep((string) segElem.Attribute("representation"));
				FeatureStruct segFS = table.GetSymbolFeatureStruct(strRep);
				if (fs == null)
					fs = segFS.DeepClone();
				else
					fs.Union(segFS);
			}

			_natClasses[id] = fs;
		}

		private FeatureStruct LoadPhoneticFeatureStruct(XElement elem)
		{
			var fs = new FeatureStruct();
			fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
			foreach (XElement featValElem in elem.Elements("FeatureValuePair").Where(IsActive))
			{
				var feature = (SymbolicFeature) GetFeature(_language.PhoneticFeatureSystem, (string) featValElem.Attribute("feature"));
				FeatureSymbol symbol = GetFeatureSymbol(feature, (string) featValElem.Attribute("value"));
				fs.AddValue(feature, new SymbolicFeatureValue(symbol));
			}
			return fs;
		}

		void LoadPRule(XmlElement pruleNode)
		{
			string id = pruleNode.GetAttribute("id");
			string name = pruleNode.SelectSingleNode("Name").InnerText;
			RewriteRule prule = new RewriteRule(id, name, _curMorpher);
			prule.MultApplication = GetMultAppOrder(pruleNode.GetAttribute("multipleApplicationOrder"));
			Dictionary<string, string> varFeatIds;
			prule.AlphaVariables = LoadVariables(pruleNode.SelectSingleNode("VariableFeatures") as XmlElement,
				out varFeatIds);
			XmlElement pseqElem = pruleNode.SelectSingleNode("PhoneticInputSequence/PhoneticSequence") as XmlElement;
			prule.Lhs = new Pattern<,>(true);
			LoadPatternNodes(prule.Lhs, pseqElem, prule.AlphaVariables, varFeatIds, null);

			XmlNodeList subruleList = pruleNode.SelectNodes("PhonologicalSubrules/PhonologicalSubrule");
			foreach (XmlNode subruleNode in subruleList)
				LoadPSubrule(subruleNode as XmlElement, prule, varFeatIds);

			_curMorpher.AddPhonologicalRule(prule);
			string[] stratumIds = pruleNode.GetAttribute("ruleStrata").Split(' ');
			foreach (string stratumId in stratumIds)
				GetStratum(stratumId).AddPhonologicalRule(prule);
		}

		void LoadPSubrule(XmlElement psubruleNode, RewriteRule prule, Dictionary<string, string> varFeatIds)
		{
			XmlElement structElem = psubruleNode.SelectSingleNode("PhonologicalSubruleStructure[@isActive='yes']") as XmlElement;
			Pattern rhs = new Pattern(true);
			LoadPatternNodes(rhs, structElem.SelectSingleNode("PhoneticOutput/PhoneticSequence") as XmlElement, prule.AlphaVariables,
				varFeatIds, null);

			AllomorphEnvironment env = LoadAllomorphEnvironment(structElem.SelectSingleNode("Environment"), prule.AlphaVariables, varFeatIds);

			RewriteRule.Subrule sr = null;
			try
			{
				sr = new RewriteRule.Subrule(rhs, env, prule);
			}
			catch (ArgumentException ae)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.InvalidSubruleType, this,
					HCStrings.kstidInvalidSubruleType, ae);
				le.Data["rule"] = prule.ID;
				throw le;
			}

			sr.RequiredPartsOfSpeech = LoadPOSs(psubruleNode.GetAttribute("requiredPartsOfSpeech"));

			sr.RequiredMprFeatures = LoadMprFeatures(psubruleNode.GetAttribute("requiredMPRFeatures"));
			sr.ExcludedMprFeatures = LoadMprFeatures(psubruleNode.GetAttribute("excludedMPRFeatures"));

			prule.AddSubrule(sr);
		}

		void LoadMetathesisRule(XmlElement metathesisNode)
		{
			string id = metathesisNode.GetAttribute("id");
			string name = metathesisNode.SelectSingleNode("Name").InnerText;
			MetathesisRule metathesisRule = new MetathesisRule(id, name, _curMorpher);
			metathesisRule.MultApplication = GetMultAppOrder(metathesisNode.GetAttribute("multipleApplicationOrder"));

			string[] changeIds = metathesisNode.GetAttribute("structuralChange").Split(' ');
			Dictionary<string, int> partIds = new Dictionary<string, int>();
			int partition = 0;
			foreach (string changeId in changeIds)
				partIds[changeId] = partition++;

			metathesisRule.Pattern = LoadPattern(metathesisNode.SelectSingleNode("StructuralDescription/PhoneticTemplate") as XmlElement,
				null, null, partIds);

			_curMorpher.AddPhonologicalRule(metathesisRule);
			string[] stratumIds = metathesisNode.GetAttribute("ruleStrata").Split(' ');
			foreach (string stratumId in stratumIds)
				GetStratum(stratumId).AddPhonologicalRule(metathesisRule);
		}

		private AffixProcessRule LoadAffixProcessRule(XElement mruleElem)
		{
			var mrule = new AffixProcessRule((string) mruleElem.Attribute("id"))
			            	{
			            		Description = (string) mruleElem.Element("Name"),
								Gloss = (string) mruleElem.Element("Gloss"),
								Blockable = (string) mruleElem.Attribute("blockable") == "true"
			            	};
			var multApp = (string) mruleElem.Attribute("multipleApplication");
			if (!string.IsNullOrEmpty(multApp))
				mrule.MaxApplicationCount = int.Parse(multApp);

			var requiredPos = (string) mruleElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				mrule.RequiredSyntacticFeatureStruct.AddValue(_posFeature, ParsePartsOfSpeech(requiredPos));

			var outPos = (string) mruleElem.Attribute("outputPartOfSpeech");
			if (!string.IsNullOrEmpty(outPos))
				mrule.OutSyntacticFeatureStruct.AddValue(_posFeature, ParsePartsOfSpeech(outPos));

			XElement requiredHeadFeatElem = mruleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				mrule.RequiredSyntacticFeatureStruct.AddValue(_headFeature, LoadSyntacticFeatureStruct(requiredHeadFeatElem));
			XElement requiredFootFeatElem = mruleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				mrule.RequiredSyntacticFeatureStruct.AddValue(_footFeature, LoadSyntacticFeatureStruct(requiredFootFeatElem));

			XElement outHeadFeatElem = mruleElem.Element("OutputHeadFeatures");
			if (outHeadFeatElem != null)
				mrule.OutSyntacticFeatureStruct.AddValue(_headFeature, LoadSyntacticFeatureStruct(outHeadFeatElem));
			XElement outFootFeatElem = mruleElem.Element("OutputFootFeatures");
			if (outFootFeatElem != null)
				mrule.OutSyntacticFeatureStruct.AddValue(_footFeature, LoadSyntacticFeatureStruct(outFootFeatElem));

			var obligHeadIDsStr = (string) mruleElem.Attribute("outputObligatoryFeatures");
			if (!string.IsNullOrEmpty(obligHeadIDsStr))
			{
				foreach (string obligHeadID in obligHeadIDsStr.Split(' '))
					mrule.ObligatorySyntacticFeatures.Add(GetFeature(_language.SyntacticFeatureSystem, obligHeadID));
			}

			foreach (XElement subruleElem in mruleElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubruleStructure").Where(IsActive))
			{
				try
				{
					mrule.Allomorphs.Add(LoadAffixProcessAllomorph(subruleElem, mrule.ID));
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			return mrule;
		}

		private RealizationalAffixProcessRule LoadRealizationalRule(XElement realRuleElem)
		{
			var realRule = new RealizationalAffixProcessRule((string) realRuleElem.Attribute("id"))
							{
								Description = (string) realRuleElem.Element("Name"),
								Gloss = (string) realRuleElem.Element("Gloss"),
								Blockable = (string) realRuleElem.Attribute("blockable") == "true"
							};

			XElement requiredHeadFeatElem = realRuleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				realRule.RequiredSyntacticFeatureStruct.AddValue(_headFeature, LoadSyntacticFeatureStruct(requiredHeadFeatElem));
			XElement requiredFootFeatElem = realRuleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				realRule.RequiredSyntacticFeatureStruct.AddValue(_footFeature, LoadSyntacticFeatureStruct(requiredFootFeatElem));
			XElement realFeatElem = realRuleElem.Element("RealizationalFeatures");
			if (realFeatElem != null)
				realRule.RealizationalFeatureStruct.AddValue(_headFeature, LoadSyntacticFeatureStruct(realFeatElem));

			foreach (XElement subruleElem in realRuleElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubruleStructure").Where(IsActive))
			{
				try
				{
					realRule.Allomorphs.Add(LoadAffixProcessAllomorph(subruleElem, realRule.ID));
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			return realRule;
		}

		private AffixProcessAllomorph LoadAffixProcessAllomorph(XElement msubruleElem, string mruleID)
		{
			var allomorph = new AffixProcessAllomorph((string) msubruleElem.Attribute("id"));

			LoadAllomorphEnvironments(msubruleElem.Element("RequiredEnvironments"), allomorph.RequiredEnvironments);
			LoadAllomorphEnvironments(msubruleElem.Element("ExcludedEnvironments"), allomorph.ExcludedEnvironments);

			LoadProperties(msubruleElem.Element("Properties"), allomorph.Properties);

			Dictionary<string, Tuple<string, SymbolicFeature>> variables = LoadVariables(msubruleElem.Element("VariableFeatures"));

			XElement inputElem = msubruleElem.Element("InputSideRecordStructure");
			Debug.Assert(inputElem != null);

			LoadMprFeatures((string) inputElem.Attribute("requiredMPRFeatures"), allomorph.RequiredMprFeatures);
			LoadMprFeatures((string) inputElem.Attribute("excludedMPRFeatures"), allomorph.ExcludedMprFeatures);
			LoadMprFeatures((string) inputElem.Attribute("MPRFeatures"), allomorph.OutMprFeatures);

			LoadMorphologicalLhs(inputElem.Element("RequiredPhoneticInput"), variables, allomorph.Lhs);

			XElement outputElem = msubruleElem.Element("OutputSideRecordStructure");
			Debug.Assert(outputElem != null);

			allomorph.ReduplicationHint = GetReduplicationHint((string) outputElem.Attribute("redupMorphType"));
			LoadMorphologicalRhs(outputElem.Element("MorphologicalPhoneticOutput"), variables, mruleID, allomorph.Rhs);

			return allomorph;
		}

		private void LoadMorphologicalLhs(XElement reqPhonInputElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables, IList<Pattern<Word, ShapeNode>> lhs)
		{
			foreach (XElement pseqElem in reqPhonInputElem.Elements("PhoneticSequence"))
				lhs.Add(new Pattern<Word, ShapeNode>((string) pseqElem.Attribute("id"), LoadPatternNodes(pseqElem, variables)));
		}

		private void LoadMorphologicalRhs(XElement phonOutputElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables, string ruleID, IList<MorphologicalOutputAction> rhs)
		{
			foreach (XElement partElem in phonOutputElem.Elements())
			{
				switch (partElem.Name.LocalName)
				{
					case "CopyFromInput":
						rhs.Add(new CopyFromInput((string) partElem.Attribute("index")));
						break;

					case "InsertSimpleContext":
						rhs.Add(new InsertShapeNode(LoadNaturalClassFeatureStruct(partElem.Element("SimpleContext"), variables)));
						break;

					case "ModifyFromInput":
						rhs.Add(new ModifyFromInput((string) partElem.Attribute("index"), LoadNaturalClassFeatureStruct(partElem.Element("SimpleContext"), variables)));
						break;

					case "InsertSegments":
						SymbolTable table = GetTable((string) partElem.Attribute("characterTable"));
						var shapeStr = (string) partElem.Element("PhoneticShape");
						Shape shape;
						if (!table.ToShape(shapeStr, out shape))
						{
							throw new LoadException(LoadError.InvalidShape,
								string.Format("Failure to translate shape '{0}' of rule '{1}' into a phonetic shape using character table '{2}'.", shapeStr, ruleID, table.ID));
						}
						rhs.Add(new InsertShape(shape));
						break;
				}
			}
		}

		private CompoundingRule LoadCompoundingRule(XElement compRuleElem)
		{
			var compRule = new CompoundingRule((string) compRuleElem.Attribute("id"))
			            	{
			            		Description = (string) compRuleElem.Element("Name"),
								Blockable = (string) compRuleElem.Attribute("blockable") == "true"
			            	};
			var multApp = (string) compRuleElem.Attribute("multipleApplication");
			if (!string.IsNullOrEmpty(multApp))
				compRule.MaxApplicationCount = int.Parse(multApp);

			var headRequiredPos = (string) compRuleElem.Attribute("headPartsOfSpeech");
			if (!string.IsNullOrEmpty(headRequiredPos))
				compRule.HeadRequiredSyntacticFeatureStruct.AddValue(_posFeature, ParsePartsOfSpeech(headRequiredPos));

			var nonHeadRequiredPos = (string) compRuleElem.Attribute("nonheadPartsOfSpeech");
			if (!string.IsNullOrEmpty(nonHeadRequiredPos))
				compRule.NonHeadRequiredSyntacticFeatureStruct.AddValue(_posFeature, ParsePartsOfSpeech(nonHeadRequiredPos));

			var outPos = (string) compRuleElem.Attribute("outputPartOfSpeech");
			if (!string.IsNullOrEmpty(outPos))
				compRule.OutSyntacticFeatureStruct.AddValue(_posFeature, ParsePartsOfSpeech(outPos));

			XElement headRequiredHeadFeatElem = compRuleElem.Element("HeadRequiredHeadFeatures");
			if (headRequiredHeadFeatElem != null)
				compRule.HeadRequiredSyntacticFeatureStruct.AddValue(_headFeature, LoadSyntacticFeatureStruct(headRequiredHeadFeatElem));
			XElement headRequiredFootFeatElem = compRuleElem.Element("HeadRequiredFootFeatures");
			if (headRequiredFootFeatElem != null)
				compRule.HeadRequiredSyntacticFeatureStruct.AddValue(_footFeature, LoadSyntacticFeatureStruct(headRequiredFootFeatElem));

			XElement nonHeadRequiredHeadFeatElem = compRuleElem.Element("NonHeadRequiredHeadFeatures");
			if (nonHeadRequiredHeadFeatElem != null)
				compRule.NonHeadRequiredSyntacticFeatureStruct.AddValue(_headFeature, LoadSyntacticFeatureStruct(nonHeadRequiredHeadFeatElem));
			XElement nonHeadRequiredFootFeatElem = compRuleElem.Element("NonHeadRequiredFootFeatures");
			if (nonHeadRequiredFootFeatElem != null)
				compRule.NonHeadRequiredSyntacticFeatureStruct.AddValue(_footFeature, LoadSyntacticFeatureStruct(nonHeadRequiredFootFeatElem));

			XElement outHeadFeatElem = compRuleElem.Element("OutputHeadFeatures");
			if (outHeadFeatElem != null)
				compRule.OutSyntacticFeatureStruct.AddValue(_headFeature, LoadSyntacticFeatureStruct(outHeadFeatElem));
			XElement outFootFeatElem = compRuleElem.Element("OutputFootFeatures");
			if (outFootFeatElem != null)
				compRule.OutSyntacticFeatureStruct.AddValue(_footFeature, LoadSyntacticFeatureStruct(outFootFeatElem));

			var obligHeadIDsStr = (string) compRuleElem.Attribute("outputObligatoryFeatures");
			if (!string.IsNullOrEmpty(obligHeadIDsStr))
			{
				foreach (string obligHeadID in obligHeadIDsStr.Split(' '))
					compRule.ObligatorySyntacticFeatures.Add(GetFeature(_language.SyntacticFeatureSystem, obligHeadID));
			}

			foreach (XElement subruleElem in compRuleElem.Elements("CompoundSubrules").Elements("CompoundSubruleStructure").Where(IsActive))
			{
				try
				{
					compRule.Subrules.Add(LoadCompoundSubrule(subruleElem, compRule.ID));
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}
			return compRule;
		}

		private CompoundingSubrule LoadCompoundSubrule(XElement compSubruleElem, string compRuleID)
		{
			string id = compSubruleNode.GetAttribute("id");
			Dictionary<string, string> varFeatIds;
			AlphaVariables varFeats = LoadVariables(compSubruleNode.SelectSingleNode("VariableFeatures"), out varFeatIds);

			XmlElement headElem = compSubruleNode.SelectSingleNode("HeadRecordStructure") as XmlElement;

			Dictionary<string, int> partIds = new Dictionary<string, int>();
			List<Pattern> headLhsList = LoadMorphologicalLhs(headElem.SelectSingleNode("RequiredPhoneticInput"), 0,
				varFeats, varFeatIds, partIds);

			List<Pattern> nonHeadLhsList = LoadMorphologicalLhs(compSubruleNode.SelectSingleNode("NonHeadRecordStructure/RequiredPhoneticInput"),
				headLhsList.Count, varFeats, varFeatIds, partIds);

			XmlElement outputElem = compSubruleNode.SelectSingleNode("OutputSideRecordStructure") as XmlElement;

			List<MorphologicalOutputAction> rhsList = LoadMorphologicalRhs(outputElem.SelectSingleNode("MorphologicalPhoneticOutput"), varFeats,
				varFeatIds, partIds, compRule.ID);

			CompoundingRule.Subrule sr = new CompoundingRule.Subrule(id, id, _curMorpher,
				headLhsList, nonHeadLhsList, rhsList, varFeats);

			sr.RequiredMPRFeatures = LoadMprFeatures(headElem.GetAttribute("requiredMPRFeatures"));
			sr.ExcludedMPRFeatures = LoadMprFeatures(headElem.GetAttribute("excludedMPRFeatures"));
			sr.OutputMPRFeatures = LoadMprFeatures(outputElem.GetAttribute("MPRFeatures"));

			sr.Properties = LoadProperties(compSubruleNode.SelectSingleNode("Properties"));

			compRule.AddSubrule(sr);
		}

		void LoadAffixTemplate(XmlElement tempNode, IDBearerSet<MorphologicalRule> templateRules)
		{
			string id = tempNode.GetAttribute("id");
			string name = tempNode.SelectSingleNode("Name").InnerText;
			AffixTemplate template = new AffixTemplate(id, name, _curMorpher);

			string posIdsStr = tempNode.GetAttribute("requiredPartsOfSpeech");
			template.RequiredPartsOfSpeech = LoadPOSs(posIdsStr);

			XmlNodeList slotList = tempNode.SelectNodes("Slot[@isActive='yes']");
			foreach (XmlNode slotNode in slotList)
			{
				XmlElement slotElem = slotNode as XmlElement;
				string slotId = slotElem.GetAttribute("id");
				string slotName = slotElem.SelectSingleNode("Name").InnerText;

				AffixTemplateSlot slot = new AffixTemplateSlot(slotId, slotName, _curMorpher);
				string ruleIdsStr = slotElem.GetAttribute("morphologicalRules");
				string[] ruleIds = ruleIdsStr.Split(' ');
				MorphologicalRule lastRule = null;
				foreach (string ruleId in ruleIds)
				{
					MorphologicalRule rule = _curMorpher.GetMorphologicalRule(ruleId);
					if (rule != null)
					{
						slot.AddRule(rule);
						lastRule = rule;
						templateRules.Add(rule);
					}
					else
					{
						if (m_quitOnError)
							throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownMRule, ruleId), ruleId);
					}
				}

				string optionalStr = slotElem.GetAttribute("optional");
				if (string.IsNullOrEmpty(optionalStr) && lastRule is RealizationalRule)
					slot.Optional = (lastRule as RealizationalRule).RealizationalFeatures.NumFeatures > 0;
				else
					slot.Optional = optionalStr == "true";
				template.AddSlot(slot);
			}

			_curMorpher.AddAffixTemplate(template);
		}

		private IEnumerable<FeatureSymbol> ParsePartsOfSpeech(string posIdsStr)
		{
			if (!string.IsNullOrEmpty(posIdsStr))
			{
				string[] posIDs = posIdsStr.Split(' ');
				foreach (string posID in posIDs)
				{
					FeatureSymbol pos;
					if (!_language.SyntacticFeatureSystem.TryGetSymbol(posID, out pos))
						throw new LoadException(LoadError.UndefinedObject, string.Format("POS '{0}' is unknown.", posID));
					yield return pos;
				}
			}
		}

		private void LoadMprFeatures(string mprFeatIDsStr, MprFeatureSet mprFeatures)
		{
			if (string.IsNullOrEmpty(mprFeatIDsStr))
				return;

			foreach (string mprFeatID in mprFeatIDsStr.Split(' '))
			{
				MprFeature mprFeature;
				if (!_mprFeatures.TryGetValue(mprFeatID, out mprFeature))
					throw new LoadException(LoadError.UndefinedObject, string.Format("MPR Feature '{0}' is unknown.", mprFeatID));
				mprFeatures.Add(mprFeature);
			}
		}

		private Dictionary<string, Tuple<string, SymbolicFeature>> LoadVariables(XElement alphaVarsElem)
		{
			var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();
			if (alphaVarsElem != null)
			{
				foreach (XElement varFeatElem in alphaVarsElem.Elements("VariableFeature"))
				{
					var varName = (string)varFeatElem.Attribute("name");
					var feature = (SymbolicFeature)GetFeature(_language.PhoneticFeatureSystem, (string)varFeatElem.Attribute("phonologicalFeature"));
					variables[(string)varFeatElem.Attribute("id")] = Tuple.Create(varName, feature);
				}
			}
			return variables;
		}

		private IEnumerable<PatternNode<Word, ShapeNode>> LoadPatternNodes(XElement pseqElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables)
		{
			foreach (XElement recElem in pseqElem.Elements())
			{
				PatternNode<Word, ShapeNode> node = null;
				switch (recElem.Name.LocalName)
				{
					case "SimpleContext":
						node = new Constraint<Word, ShapeNode>(LoadNaturalClassFeatureStruct(recElem, variables));
						break;

					case "Segment":
					case "BoundaryMarker":
						SymbolTable symTable = GetTable((string) recElem.Attribute("characterTable"));
						string strRep = GetStrRep((string) recElem.Attribute("representation"));
						node = new Constraint<Word, ShapeNode>(symTable.GetSymbolFeatureStruct(strRep));
						break;

					case "OptionalSegmentSequence":
						var minStr = (string) recElem.Attribute("min");
						int min = string.IsNullOrEmpty(minStr) ? 0 : int.Parse(minStr);
						var maxStr = (string) recElem.Attribute("max");
						int max = string.IsNullOrEmpty(maxStr) ? -1 : int.Parse(maxStr);
						node = new Quantifier<Word, ShapeNode>(min, max, new Group<Word, ShapeNode>(LoadPatternNodes(recElem, variables)));
						break;

					case "Segments":
						SymbolTable segsTable = GetTable((string) recElem.Attribute("characterTable"));
						var shapeStr = (string) recElem.Element("PhoneticShape");
						Shape shape;
						if (!segsTable.ToShape(shapeStr, out shape))
						{
							throw new LoadException(LoadError.InvalidShape,
								string.Format("Failure to translate shape '{0}' in a phonetic sequence into a phonetic shape using character table '{1}'.", shapeStr, table.ID));
						}
						node = new Group<Word, ShapeNode>(shape.Select(n => new Constraint<Word, ShapeNode>(n.Annotation.FeatureStruct)));
						break;
				}

				var id = (string) recElem.Attribute("id");
				yield return string.IsNullOrEmpty(id) ? node : new Group<Word, ShapeNode>(id, node);
			}
		}

		private FeatureStruct LoadNaturalClassFeatureStruct(XElement ctxtElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables)
		{
			var natClassID = (string) ctxtElem.Attribute("naturalClass");
			FeatureStruct fs;
			if (!_natClasses.TryGetValue(natClassID, out fs))
				throw new LoadException(LoadError.UndefinedObject, string.Format("Natural class '{0}' is unknown.", natClassID));

			fs = fs.DeepClone();
			foreach (XElement varElem in ctxtElem.Elements("AlphaVariables").Elements("AlphaVariable"))
			{
				var varID = (string)varElem.Attribute("variableFeature");
				Tuple<string, SymbolicFeature> variable;
				if (!variables.TryGetValue(varID, out variable))
					throw new LoadException(LoadError.UndefinedObject, string.Format("Variable '{0}' is unknown.", varID));
				fs.AddValue(variable.Item2, new SymbolicFeatureValue(variable.Item2, variable.Item1, (string)varElem.Attribute("polarity") == "plus"));
			}
			return fs;
		}

		private Pattern<Word, ShapeNode> LoadPattern(XElement ptempElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables)
		{
			if (ptempElem == null)
				return null;
			var pattern = new Pattern<Word, ShapeNode>();
			if ((string) ptempElem.Attribute("initialBoundaryCondition") == "true")
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
			foreach (PatternNode<Word, ShapeNode> node in LoadPatternNodes(ptempElem.Element("PhoneticSequence"), variables))
				pattern.Children.Add(node);
			if ((string) ptempElem.Attribute("finalBoundaryCondition") == "true")
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));

			return pattern;
		}

		Stratum GetStratum(string id)
		{
			Stratum stratum = _curMorpher.GetStratum(id);
			if (stratum == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownStratum, id), id);
			return stratum;
		}

		private SymbolTable GetTable(string id)
		{
			SymbolTable table;
			if (!_tables.TryGetValue(id, out table))
				throw new LoadException(LoadError.UndefinedObject, string.Format("Character definition table {0} is unknown.", id));
			return table;
		}

		private string GetStrRep(string id)
		{
			string strRep;
			if (!_repIds.TryGetValue(id, out strRep))
				throw new LoadException(LoadError.UndefinedObject, string.Format("Character definition {0} is unknown.", id));
			return strRep;
		}

		private Feature GetFeature(FeatureSystem featSys, string id)
		{
			Feature feature;
			if (!featSys.TryGetFeature(id, out feature))
				throw new LoadException(LoadError.UndefinedObject, string.Format("Unknown feature '{0}'.", id));
			return feature;
		}

		private FeatureSymbol GetFeatureSymbol(SymbolicFeature feature, string id)
		{
			FeatureSymbol symbol;
			if (!feature.PossibleSymbols.TryGetValue(id, out symbol))
				throw new LoadException(LoadError.UndefinedObject, string.Format("Unknown value '{0}' for feature '{1}'.", id, feature.ID));
			return symbol;
		}
	}
}
