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

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents the loader for HC.NET's XML input format.
	/// </summary>
	public class XmlLoader
	{
		public static Language Load(string configPath)
		{
			return Load(configPath, false);
		}

		public static Language Load(string configPath, bool quitOnError)
		{
			return Load(configPath, quitOnError, null);
		}

		public static Language Load(string configPath, bool quitOnError, XmlResolver resolver)
		{
			var loader = new XmlLoader(configPath, quitOnError, resolver);
			return loader.Load();
		}

		private static MorphologicalRuleOrder GetMorphologicalRuleOrder(string ruleOrderStr)
		{
			switch (ruleOrderStr)
			{
				case "linear":
					return MorphologicalRuleOrder.Linear;

				case "unordered":
					return MorphologicalRuleOrder.Unordered;
			}

			return MorphologicalRuleOrder.Unordered;
		}

		private static RewriteApplicationMode GetApplicationMode(string multAppOrderStr)
		{
			switch (multAppOrderStr)
			{
				case "simultaneous":
					return RewriteApplicationMode.Simultaneous;

				case "rightToLeftIterative":
				case "leftToRightIterative":
					return RewriteApplicationMode.Iterative;
			}

			return RewriteApplicationMode.Iterative;
		}

		private static Direction GetDirection(string multAppOrderStr)
		{
			switch (multAppOrderStr)
			{
				case "rightToLeftIterative":
					return Direction.RightToLeft;
				case "leftToRightIterative":
					return Direction.LeftToRight;
			}

			return Direction.LeftToRight;
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
		private readonly IDBearerSet<IMorphologicalRule> _mrules;
		private readonly IDBearerSet<AffixTemplate> _templates;
		private readonly IDBearerSet<IMorphologicalRule> _templateRules; 
		private readonly IDBearerSet<Stratum> _strata;
		private readonly IDBearerSet<LexFamily> _families;
		private readonly IDBearerSet<LexEntry> _entries;
		private readonly IDBearerSet<Morpheme> _morphemes;
		private readonly IDBearerSet<Allomorph> _allomorphs;

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
			_mrules = new IDBearerSet<IMorphologicalRule>();
			_templates = new IDBearerSet<AffixTemplate>();
			_templateRules = new IDBearerSet<IMorphologicalRule>();
			_strata = new IDBearerSet<Stratum>();
			_families = new IDBearerSet<LexFamily>();
			_entries = new IDBearerSet<LexEntry>();
			_morphemes = new IDBearerSet<Morpheme>();
			_allomorphs = new IDBearerSet<Allomorph>();
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
				throw new LoadException(LoadErrorCode.ParseError, string.Format("Unable to parser input file: {0}.", _configPath), xe);
			}
			finally
			{
				reader.Close();
			}

			LoadLanguage(doc.Elements("HermitCrabInput").Elements("Language").Single(IsActive));
			return _language;
		}

		private static void ValidationEventHandler(object sender, ValidationEventArgs e)
		{
			throw new LoadException(LoadErrorCode.InvalidFormat, e.Message + " Line: " + e.Exception.LineNumber + ", Pos: " + e.Exception.LinePosition);
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

			LoadFeatureSystem(langElem.Elements("PhonologicalFeatureSystem").SingleOrDefault(IsActive), _language.PhoneticFeatureSystem);
			foreach (SymbolicFeature feature in _language.PhoneticFeatureSystem)
			{
				var unknownSymbol = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "?" };
				feature.PossibleSymbols.Add(unknownSymbol);
				feature.DefaultValue = new SymbolicFeatureValue(unknownSymbol);
			}

			_language.SyntacticFeatureSystem.Add(_headFeature);
			LoadFeatureSystem(langElem.Element("HeadFeatures"), _language.SyntacticFeatureSystem);
			_language.SyntacticFeatureSystem.Add(_footFeature);
			LoadFeatureSystem(langElem.Element("FootFeatures"), _language.SyntacticFeatureSystem);

			foreach (XElement charDefTableElem in langElem.Elements("CharacterDefinitionTable").Where(IsActive))
				LoadSymbolTable(charDefTableElem);

			foreach (XElement natClassElem in langElem.Elements("NaturalClasses").Elements("FeatureNaturalClass").Where(IsActive))
				_natClasses[(string) natClassElem.Attribute("id")] = LoadPhoneticFeatureStruct(natClassElem);

			foreach (XElement natClassElem in langElem.Elements("NaturalClasses").Elements("SegmentNaturalClass").Where(IsActive))
				LoadSegmentNaturalClass(natClassElem);

			foreach (XElement mruleElem in langElem.Elements("MorphologicalRules").Elements().Where(IsActive))
			{
				try
				{
					switch (mruleElem.Name.LocalName)
					{
						case "MorphologicalRule":
							LoadAffixProcessRule(mruleElem);
							break;

						case "RealizationalRule":
							LoadRealizationalRule(mruleElem);
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

			foreach (XElement tempElem in langElem.Elements("Strata").Elements("AffixTemplate").Where(IsActive))
				LoadAffixTemplate(tempElem);

			foreach (XElement stratumElem in langElem.Elements("Strata").Elements("Stratum").Where(IsActive))
				LoadStratum(stratumElem);

			foreach (XElement mruleElem in langElem.Elements("MorphologicalRules").Elements().Where(IsActive))
			{
				var ruleId = (string) mruleElem.Attribute("id");

				IMorphologicalRule rule;
				if (!_templateRules.Contains(ruleId) && _mrules.TryGetValue(ruleId, out rule))
				{
					Stratum stratum = GetStratum((string) mruleElem.Attribute("stratum"));
					stratum.MorphologicalRules.Add(rule);
				}
			}

			foreach (XElement familyElem in langElem.Elements("Lexicon").Elements("Families").Elements("Family").Where(IsActive))
				_families.Add(new LexFamily((string) familyElem.Attribute("id")) { Description = (string) familyElem.Element("Name") });

			foreach (XElement entryElem in langElem.Elements("Lexicon").Elements("LexicalEntry").Where(IsActive))
			{
				try
				{
					LoadLexEntry(entryElem);
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			// co-occurrence rules cannot be loaded until all of the morphemes and their allomorphs have been loaded
			foreach (XElement morphemeElem in langElem.Elements("Lexicon").Elements("LexicalEntry").Concat(langElem.Elements("MorphologicalRules").Elements()).Where(IsActive))
			{
				var morphemeID = (string) morphemeElem.Attribute("id");
				Morpheme morpheme;
				if (_morphemes.TryGetValue(morphemeID, out morpheme))
				{
					try
					{
						morpheme.RequiredMorphemeCoOccurrences.AddRange(LoadMorphemeCoOccurrenceRules(morphemeElem.Element("RequiredMorphemeCoOccurrences")));
					}
					catch (LoadException)
					{
						if (_quitOnError)
							throw;
					}
					try
					{
						morpheme.ExcludedMorphemeCoOccurrences.AddRange(LoadMorphemeCoOccurrenceRules(morphemeElem.Element("ExcludedMorphemeCoOccurrences")));
					}
					catch (LoadException)
					{
						if (_quitOnError)
							throw;
					}
				}

				foreach (XElement alloElem in morphemeElem.Elements("Allomorphs").Elements("Allomorph")
					.Concat(morphemeElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubruleStructure").Where(IsActive)))
				{
					var alloID = (string) alloElem.Attribute("id");
					Allomorph allomorph;
					if (_allomorphs.TryGetValue(alloID, out allomorph))
					{
						try
						{
							allomorph.RequiredAllomorphCoOccurrences.AddRange(LoadAllomorphCoOccurrenceRules(alloElem.Element("RequiredAllomorphCoOccurrences")));
						}
						catch (LoadException)
						{
							if (_quitOnError)
								throw;
						}
						try
						{
							allomorph.ExcludedAllomorphCoOccurrences.AddRange(LoadAllomorphCoOccurrenceRules(alloElem.Element("ExcludedAllomorphCoOccurrences")));
						}
						catch (LoadException)
						{
							if (_quitOnError)
								throw;
						}
					}
				}
			}

			foreach (XElement pruleElem in langElem.Elements("PhonologicalRules").Elements().Where(IsActive))
			{
				try
				{
					switch (pruleElem.Name.LocalName)
					{
						case "MetathesisRule":
							LoadMetathesisRule(pruleElem);
							break;

						case "PhonologicalRule":
							LoadRewriteRule(pruleElem);
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

		private void LoadStratum(XElement stratumElem)
		{
			var stratum = new Stratum((string) stratumElem.Attribute("id"), GetTable((string) stratumElem.Attribute("characterDefinitionTable")))
			              	{
			              		Description = (string) stratumElem.Element("Name"),
			              		MorphologicalRuleOrder = GetMorphologicalRuleOrder((string) stratumElem.Attribute("morphologicalRuleOrder"))
			              	};

			var tempIDsStr = (string) stratumElem.Attribute("affixTemplates");
			if (!string.IsNullOrEmpty(tempIDsStr))
			{
				foreach (string tempID in tempIDsStr.Split(' '))
				{
					AffixTemplate template;
					if (!_templates.TryGetValue(tempID, out template))
						throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Unknown affix template '{0}'.", tempID));
					stratum.AffixTemplates.Add(template);
				}
			}

			_language.Strata.Add(stratum);
			_strata.Add(stratum);
		}

		private void LoadLexEntry(XElement entryElem)
		{
			var entry = new LexEntry((string) entryElem.Attribute("id")) { Gloss = (string) entryElem.Element("Gloss") };

			var fs = new FeatureStruct();
			var pos = (string) entryElem.Attribute("partOfSpeech");
			if (!string.IsNullOrEmpty(pos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(pos));

			XElement headFeatElem = entryElem.Element("AssignedHeadFeatures");
			if (headFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(headFeatElem));
			XElement footFeatElem = entryElem.Element("AssignedFootFeatures");
			if (footFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(footFeatElem));
			fs.Freeze();
			entry.SyntacticFeatureStruct = fs;

			entry.MprFeatures.UnionWith(LoadMprFeatures((string) entryElem.Attribute("ruleFeatures")));

			var familyID = (string) entryElem.Attribute("family");
			if (!string.IsNullOrEmpty(familyID))
			{
				LexFamily family;
				if (!_families.TryGetValue(familyID, out family))
					throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Unknown lexical family '{0}'.", familyID));
				family.Entries.Add(entry);
			}

			Stratum stratum = GetStratum((string) entryElem.Attribute("stratum"));
			foreach (XElement alloElem in entryElem.Elements("Allomorphs").Elements("Allomorph").Where(IsActive))
			{
				try
				{
					RootAllomorph allomorph = LoadRootAllomorph(alloElem, stratum.SymbolTable);
					entry.Allomorphs.Add(allomorph);
					_allomorphs.Add(allomorph);
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			if (entry.Allomorphs.Count > 0)
			{
				stratum.Entries.Add(entry);
				_entries.Add(entry);
				_morphemes.Add(entry);
			}
		}

		private RootAllomorph LoadRootAllomorph(XElement alloElem, SymbolTable table)
		{
			var alloID = (string) alloElem.Attribute("id");
			var shapeStr = (string) alloElem.Element("PhoneticShape");
			Shape shape;
			if (!table.ToShape(shapeStr, out shape))
			{
				throw new LoadException(LoadErrorCode.InvalidShape,
					string.Format("Failure to translate shape '{0}' of allomorph '{1}' into a phonetic shape using character table '{2}'.", shapeStr, alloID, table.ID));
			}
			var allomorph = new RootAllomorph(alloID, shape);

			allomorph.RequiredEnvironments.AddRange(LoadAllomorphEnvironments(alloElem.Element("RequiredEnvironments")));
			allomorph.ExcludedEnvironments.AddRange(LoadAllomorphEnvironments(alloElem.Element("ExcludedEnvironments")));

			LoadProperties(alloElem.Element("Properties"), allomorph.Properties);

			return allomorph;
		}

		private IEnumerable<AllomorphEnvironment> LoadAllomorphEnvironments(XElement envsElem)
		{
			if (envsElem == null)
				yield break;

			foreach (XElement envElem in envsElem.Elements("Environment"))
				yield return LoadAllomorphEnvironment(envElem);
		}

		private AllomorphEnvironment LoadAllomorphEnvironment(XElement envElem)
		{
			var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();

			Pattern<Word, ShapeNode> leftEnv = LoadPhoneticTemplate(envElem.Elements("LeftEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables, false);
			Pattern<Word, ShapeNode> rightEnv = LoadPhoneticTemplate(envElem.Elements("RightEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables, false);
			return new AllomorphEnvironment(_spanFactory, leftEnv, rightEnv);
		}

		private IEnumerable<MorphemeCoOccurrenceRule> LoadMorphemeCoOccurrenceRules(XElement coOccursElem)
		{
			if (coOccursElem == null)
				yield break;

			foreach (XElement coOccurElem in coOccursElem.Elements("MorphemeCoOccurrence"))
				yield return LoadMorphemeCoOccurrenceRule(coOccurElem);
		}

		private MorphemeCoOccurrenceRule LoadMorphemeCoOccurrenceRule(XElement coOccurElem)
		{
			MorphCoOccurrenceAdjacency adjacency = GetAdjacencyType((string) coOccurElem.Attribute("adjacency"));
			var morphemeIDsStr = (string) coOccurElem.Attribute("morphemes");
			var morphemes = new IDBearerSet<Morpheme>();
			foreach (string morphemeID in morphemeIDsStr.Split(' '))
			{
				Morpheme morpheme;
				if (!_morphemes.TryGetValue(morphemeID, out morpheme))
					throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Unknown morpheme '{0}'.", morphemeID));
				morphemes.Add(morpheme);
			}
			return new MorphemeCoOccurrenceRule(morphemes, adjacency);
		}

		private IEnumerable<AllomorphCoOccurrenceRule> LoadAllomorphCoOccurrenceRules(XElement coOccursElem)
		{
			if (coOccursElem == null)
				yield break;

			foreach (XElement coOccurElem in coOccursElem.Elements("AllomorphCoOccurrence"))
				yield return LoadAllomorphCoOccurrenceRule(coOccurElem);
		}

		private AllomorphCoOccurrenceRule LoadAllomorphCoOccurrenceRule(XElement coOccurElem)
		{
			MorphCoOccurrenceAdjacency adjacency = GetAdjacencyType((string) coOccurElem.Attribute("adjacency"));
			var allomorphIDsStr = (string) coOccurElem.Attribute("allomorphs");
			var allomorphs = new IDBearerSet<Allomorph>();
			foreach (string allomorphID in allomorphIDsStr.Split(' '))
			{
				Allomorph allomorph;
				if (!_allomorphs.TryGetValue(allomorphID, out allomorph))
					throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Unknown allomorph '{0}'.", allomorphID));
				allomorphs.Add(allomorph);
			}
			return new AllomorphCoOccurrenceRule(allomorphs, adjacency);
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
			if (featSysElem == null)
				return;

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

		private void LoadSegmentNaturalClass(XElement natClassElem)
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
			Debug.Assert(fs != null);
			fs.Freeze();
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
			fs.Freeze();
			return fs;
		}

		private void LoadRewriteRule(XElement pruleElem)
		{
			var multAppOrderStr = (string) pruleElem.Attribute("multipleApplicationOrder");
			var prule = new RewriteRule((string) pruleElem.Attribute("id"))
			            	{
			            		Description = (string) pruleElem.Element("Name"),
			            		ApplicationMode = GetApplicationMode(multAppOrderStr),
								Direction = GetDirection(multAppOrderStr)
			            	};
			Dictionary<string, Tuple<string, SymbolicFeature>> variables = LoadVariables(pruleElem.Element("VariableFeatures"));
			prule.Lhs = LoadPhoneticSequence(pruleElem.Elements("PhoneticInputSequence").Elements("PhoneticSequence").SingleOrDefault(), variables, false);

			foreach (XElement subruleElem in pruleElem.Elements("PhonologicalSubrules").Elements("PhonologicalSubrule"))
				prule.Subrules.Add(LoadRewriteSubrule(subruleElem, variables));

			var stratumIDsStr = (string) pruleElem.Attribute("ruleStrata");
			foreach (string stratumID in stratumIDsStr.Split(' '))
				GetStratum(stratumID).PhonologicalRules.Add(prule);
		}

		private RewriteSubrule LoadRewriteSubrule(XElement psubruleElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables)
		{
			var subrule = new RewriteSubrule();

			XElement structElem = psubruleElem.Elements("PhonologicalSubruleStructure").Single(IsActive);

			var requiredPos = (string) structElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				subrule.RequiredSyntacticFeatureStruct = FeatureStruct.New().Feature(_posFeature).EqualTo(ParsePartsOfSpeech(requiredPos)).Value;

			subrule.RequiredMprFeatures.UnionWith(LoadMprFeatures((string) structElem.Attribute("requiredMPRFeatures")));
			subrule.ExcludedMprFeatures.UnionWith(LoadMprFeatures((string) structElem.Attribute("excludedMPRFeatures")));

			subrule.Rhs = LoadPhoneticSequence(structElem.Elements("PhoneticOutput").Elements("PhoneticSequence").SingleOrDefault(), variables, false);

			XElement envElem = structElem.Element("Environment");
			if (envElem != null)
			{
				subrule.LeftEnvironment = LoadPhoneticTemplate(envElem.Elements("LeftEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables, false);
				subrule.RightEnvironment = LoadPhoneticTemplate(envElem.Elements("RightEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables, false);
			}

			return subrule;
		}

		private void LoadMetathesisRule(XElement metathesisElem)
		{
			var metathesisRule = new MetathesisRule((string) metathesisElem.Attribute("id"))
			                     	{
			                     		Description = (string) metathesisElem.Element("Name"),
										Direction = GetDirection((string) metathesisElem.Attribute("multipleApplicationOrder"))
			                     	};

			var changeIDsStr = (string) metathesisElem.Attribute("structuralChange");
			metathesisRule.GroupOrder.AddRange(changeIDsStr.Split(' '));

			metathesisRule.Pattern = LoadPhoneticTemplate(metathesisElem.Elements("StructuralDescription").Elements("PhoneticTemplate").Single(),
				new Dictionary<string, Tuple<string, SymbolicFeature>>(), true);

			var stratumIDsStr = (string) metathesisElem.Attribute("ruleStrata");
			foreach (string stratumID in stratumIDsStr.Split(' '))
				GetStratum(stratumID).PhonologicalRules.Add(metathesisRule);
		}

		private void LoadAffixProcessRule(XElement mruleElem)
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

			var fs = new FeatureStruct();
			var requiredPos = (string) mruleElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(requiredPos));
			XElement requiredHeadFeatElem = mruleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(requiredHeadFeatElem));
			XElement requiredFootFeatElem = mruleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(requiredFootFeatElem));
			fs.Freeze();
			mrule.RequiredSyntacticFeatureStruct = fs;

			fs = new FeatureStruct();
			var outPos = (string) mruleElem.Attribute("outputPartOfSpeech");
			if (!string.IsNullOrEmpty(outPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(outPos));
			XElement outHeadFeatElem = mruleElem.Element("OutputHeadFeatures");
			if (outHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(outHeadFeatElem));
			XElement outFootFeatElem = mruleElem.Element("OutputFootFeatures");
			if (outFootFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(outFootFeatElem));
			fs.Freeze();
			mrule.OutSyntacticFeatureStruct = fs;

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
					AffixProcessAllomorph allomorph = LoadAffixProcessAllomorph(subruleElem, mrule.ID);
					mrule.Allomorphs.Add(allomorph);
					_allomorphs.Add(allomorph);
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			if (mrule.Allomorphs.Count > 0)
			{
				_mrules.Add(mrule);
				_morphemes.Add(mrule);
			}
		}

		private void LoadRealizationalRule(XElement realRuleElem)
		{
			var realRule = new RealizationalAffixProcessRule((string) realRuleElem.Attribute("id"))
							{
								Description = (string) realRuleElem.Element("Name"),
								Gloss = (string) realRuleElem.Element("Gloss"),
								Blockable = (string) realRuleElem.Attribute("blockable") == "true"
							};

			var fs = new FeatureStruct();
			XElement requiredHeadFeatElem = realRuleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(requiredHeadFeatElem));
			XElement requiredFootFeatElem = realRuleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(requiredFootFeatElem));
			fs.Freeze();
			realRule.RequiredSyntacticFeatureStruct = fs;

			XElement realFeatElem = realRuleElem.Element("RealizationalFeatures");
			if (realFeatElem != null)
				realRule.RealizationalFeatureStruct = FeatureStruct.New().Feature(_headFeature).EqualTo(LoadSyntacticFeatureStruct(realFeatElem)).Value;

			foreach (XElement subruleElem in realRuleElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubruleStructure").Where(IsActive))
			{
				try
				{
					AffixProcessAllomorph allomorph = LoadAffixProcessAllomorph(subruleElem, realRule.ID);
					realRule.Allomorphs.Add(allomorph);
					_allomorphs.Add(allomorph);
				}
				catch (LoadException)
				{
					if (_quitOnError)
						throw;
				}
			}

			if (realRule.Allomorphs.Count > 0)
			{
				_mrules.Add(realRule);
				_morphemes.Add(realRule);
			}
		}

		private AffixProcessAllomorph LoadAffixProcessAllomorph(XElement msubruleElem, string mruleID)
		{
			var allomorph = new AffixProcessAllomorph((string) msubruleElem.Attribute("id"));

			allomorph.RequiredEnvironments.AddRange(LoadAllomorphEnvironments(msubruleElem.Element("RequiredEnvironments")));
			allomorph.ExcludedEnvironments.AddRange(LoadAllomorphEnvironments(msubruleElem.Element("ExcludedEnvironments")));

			LoadProperties(msubruleElem.Element("Properties"), allomorph.Properties);

			Dictionary<string, Tuple<string, SymbolicFeature>> variables = LoadVariables(msubruleElem.Element("VariableFeatures"));

			XElement inputElem = msubruleElem.Element("InputSideRecordStructure");
			Debug.Assert(inputElem != null);

			allomorph.RequiredMprFeatures.UnionWith(LoadMprFeatures((string) inputElem.Attribute("requiredMPRFeatures")));
			allomorph.ExcludedMprFeatures.UnionWith(LoadMprFeatures((string)inputElem.Attribute("excludedMPRFeatures")));
			allomorph.OutMprFeatures.UnionWith(LoadMprFeatures((string)inputElem.Attribute("MPRFeatures")));

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
				lhs.Add(LoadPhoneticSequence(pseqElem, variables, true));
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
							throw new LoadException(LoadErrorCode.InvalidShape,
								string.Format("Failure to translate shape '{0}' of rule '{1}' into a phonetic shape using character table '{2}'.", shapeStr, ruleID, table.ID));
						}
						rhs.Add(new InsertShape(shape));
						break;
				}
			}
		}

		private void LoadCompoundingRule(XElement compRuleElem)
		{
			var compRule = new CompoundingRule((string) compRuleElem.Attribute("id"))
			            	{
			            		Description = (string) compRuleElem.Element("Name"),
								Blockable = (string) compRuleElem.Attribute("blockable") == "true"
			            	};
			var multApp = (string) compRuleElem.Attribute("multipleApplication");
			if (!string.IsNullOrEmpty(multApp))
				compRule.MaxApplicationCount = int.Parse(multApp);

			var fs = new FeatureStruct();
			var headRequiredPos = (string) compRuleElem.Attribute("headPartsOfSpeech");
			if (!string.IsNullOrEmpty(headRequiredPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(headRequiredPos));
			XElement headRequiredHeadFeatElem = compRuleElem.Element("HeadRequiredHeadFeatures");
			if (headRequiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(headRequiredHeadFeatElem));
			XElement headRequiredFootFeatElem = compRuleElem.Element("HeadRequiredFootFeatures");
			if (headRequiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(headRequiredFootFeatElem));
			fs.Freeze();
			compRule.HeadRequiredSyntacticFeatureStruct = fs;

			fs = new FeatureStruct();
			var nonHeadRequiredPos = (string) compRuleElem.Attribute("nonheadPartsOfSpeech");
			if (!string.IsNullOrEmpty(nonHeadRequiredPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(nonHeadRequiredPos));
			XElement nonHeadRequiredHeadFeatElem = compRuleElem.Element("NonHeadRequiredHeadFeatures");
			if (nonHeadRequiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(nonHeadRequiredHeadFeatElem));
			XElement nonHeadRequiredFootFeatElem = compRuleElem.Element("NonHeadRequiredFootFeatures");
			if (nonHeadRequiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(nonHeadRequiredFootFeatElem));
			fs.Freeze();
			compRule.NonHeadRequiredSyntacticFeatureStruct = fs;

			fs = new FeatureStruct();
			var outPos = (string) compRuleElem.Attribute("outputPartOfSpeech");
			if (!string.IsNullOrEmpty(outPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(outPos));
			XElement outHeadFeatElem = compRuleElem.Element("OutputHeadFeatures");
			if (outHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(outHeadFeatElem));
			XElement outFootFeatElem = compRuleElem.Element("OutputFootFeatures");
			if (outFootFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(outFootFeatElem));
			fs.Freeze();
			compRule.OutSyntacticFeatureStruct = fs;

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

			if (compRule.Subrules.Count > 0)
				_mrules.Add(compRule);
		}

		private CompoundingSubrule LoadCompoundSubrule(XElement compSubruleElem, string compRuleID)
		{
			var subrule = new CompoundingSubrule((string) compSubruleElem.Attribute("id"));

			Dictionary<string, Tuple<string, SymbolicFeature>> variables = LoadVariables(compSubruleElem.Element("VariableFeatures"));

			XElement headElem = compSubruleElem.Element("HeadRecordStructure");
			Debug.Assert(headElem != null);

			subrule.RequiredMprFeatures.UnionWith(LoadMprFeatures((string) headElem.Attribute("requiredMPRFeatures")));
			subrule.ExcludedMprFeatures.UnionWith(LoadMprFeatures((string) headElem.Attribute("excludedMPRFeatures")));
			subrule.OutMprFeatures.UnionWith(LoadMprFeatures((string) headElem.Attribute("MPRFeatures")));
			
			LoadMorphologicalLhs(headElem.Element("RequiredPhoneticInput"), variables, subrule.HeadLhs);

			XElement nonHeadElem = compSubruleElem.Element("NonHeadRecordStructure");
			Debug.Assert(nonHeadElem != null);

			LoadMorphologicalLhs(nonHeadElem.Element("RequiredPhoneticInput"), variables, subrule.NonHeadLhs);

			XElement outputElem = compSubruleElem.Element("OutputSideRecordStructure");
			Debug.Assert(outputElem != null);

			LoadMorphologicalRhs(outputElem.Element("MorphologicalPhoneticOutput"), variables, compRuleID, subrule.Rhs);

			return subrule;
		}

		private void LoadAffixTemplate(XElement tempElem)
		{
			var template = new AffixTemplate((string) tempElem.Attribute("id")) { Description = (string) tempElem.Element("Name") };

			var requiredPos = (string) tempElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				template.RequiredSyntacticFeatureStruct = FeatureStruct.New().Feature(_posFeature).EqualTo(ParsePartsOfSpeech(requiredPos)).Value;

			foreach (XElement slotElem in tempElem.Elements("Slot").Where(IsActive))
			{
				var slot = new AffixTemplateSlot((string) slotElem.Attribute("id")) { Description = (string) slotElem.Element("Name") };
				var ruleIDsStr = (string) slotElem.Attribute("morphologicalRules");
				IMorphologicalRule lastRule = null;
				foreach (string ruleID in ruleIDsStr.Split(' '))
				{
					IMorphologicalRule rule;
					if (_mrules.TryGetValue(ruleID, out rule))
					{
						slot.Rules.Add(rule);
						_templateRules.Add(rule);
						lastRule = rule;
					}
					else
					{
						if (_quitOnError)
							throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Unknown morphological rule '{0}'.", ruleID));
					}
				}

				var optionalStr = (string) slotElem.Attribute("optional");
				var realRule = lastRule as RealizationalAffixProcessRule;
				if (string.IsNullOrEmpty(optionalStr) && realRule != null)
					slot.Optional = !realRule.RealizationalFeatureStruct.IsEmpty;
				else
					slot.Optional = optionalStr == "true";
				template.Slots.Add(slot);
			}

			_templates.Add(template);
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
						throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("POS '{0}' is unknown.", posID));
					yield return pos;
				}
			}
		}

		private IEnumerable<MprFeature> LoadMprFeatures(string mprFeatIDsStr)
		{
			if (string.IsNullOrEmpty(mprFeatIDsStr))
				yield break;

			foreach (string mprFeatID in mprFeatIDsStr.Split(' '))
			{
				MprFeature mprFeature;
				if (!_mprFeatures.TryGetValue(mprFeatID, out mprFeature))
					throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("MPR Feature '{0}' is unknown.", mprFeatID));
				yield return mprFeature;
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

		private IEnumerable<PatternNode<Word, ShapeNode>> LoadPatternNodes(XElement pseqElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables, bool createGroups)
		{
			foreach (XElement recElem in pseqElem.Elements())
			{
				IEnumerable<PatternNode<Word, ShapeNode>> nodes = null;
				switch (recElem.Name.LocalName)
				{
					case "SimpleContext":
						nodes = new Constraint<Word, ShapeNode>(LoadNaturalClassFeatureStruct(recElem, variables)).ToEnumerable();
						break;

					case "Segment":
					case "BoundaryMarker":
						SymbolTable symTable = GetTable((string) recElem.Attribute("characterTable"));
						string strRep = GetStrRep((string) recElem.Attribute("representation"));
						nodes = new Constraint<Word, ShapeNode>(symTable.GetSymbolFeatureStruct(strRep)).ToEnumerable();
						break;

					case "OptionalSegmentSequence":
						var minStr = (string) recElem.Attribute("min");
						int min = string.IsNullOrEmpty(minStr) ? 0 : int.Parse(minStr);
						var maxStr = (string) recElem.Attribute("max");
						int max = string.IsNullOrEmpty(maxStr) ? -1 : int.Parse(maxStr);
						nodes = new Quantifier<Word, ShapeNode>(min, max, new Group<Word, ShapeNode>(LoadPatternNodes(recElem, variables, false))).ToEnumerable();
						break;

					case "Segments":
						SymbolTable segsTable = GetTable((string) recElem.Attribute("characterTable"));
						var shapeStr = (string) recElem.Element("PhoneticShape");
						Shape shape;
						if (!segsTable.ToShape(shapeStr, out shape))
						{
							throw new LoadException(LoadErrorCode.InvalidShape,
								string.Format("Failure to translate shape '{0}' in a phonetic sequence into a phonetic shape using character table '{1}'.", shapeStr, segsTable.ID));
						}
						nodes = shape.Select(n => new Constraint<Word, ShapeNode>(n.Annotation.FeatureStruct));
						break;
				}

				Debug.Assert(nodes != null);
				var id = (string) recElem.Attribute("id");
				if (!createGroups || string.IsNullOrEmpty(id))
				{
					foreach (PatternNode<Word, ShapeNode> node in nodes)
						yield return node;
				}
				else
				{
					yield return new Group<Word, ShapeNode>(id, nodes);
				}
			}
		}

		private FeatureStruct LoadNaturalClassFeatureStruct(XElement ctxtElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables)
		{
			var natClassID = (string) ctxtElem.Attribute("naturalClass");
			FeatureStruct fs;
			if (!_natClasses.TryGetValue(natClassID, out fs))
				throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Natural class '{0}' is unknown.", natClassID));

			fs = fs.DeepClone();
			foreach (XElement varElem in ctxtElem.Elements("AlphaVariables").Elements("AlphaVariable"))
			{
				var varID = (string)varElem.Attribute("variableFeature");
				Tuple<string, SymbolicFeature> variable;
				if (!variables.TryGetValue(varID, out variable))
					throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Variable '{0}' is unknown.", varID));
				fs.AddValue(variable.Item2, new SymbolicFeatureValue(variable.Item2, variable.Item1, (string)varElem.Attribute("polarity") == "plus"));
			}
			return fs;
		}

		private Pattern<Word, ShapeNode> LoadPhoneticTemplate(XElement ptempElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables, bool createGroups)
		{
			var pattern = new Pattern<Word, ShapeNode>();
			if (ptempElem != null)
			{
				if ((string) ptempElem.Attribute("initialBoundaryCondition") == "true")
					pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
				foreach (PatternNode<Word, ShapeNode> node in LoadPatternNodes(ptempElem.Element("PhoneticSequence"), variables, createGroups))
					pattern.Children.Add(node);
				if ((string) ptempElem.Attribute("finalBoundaryCondition") == "true")
					pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
			}
			pattern.Freeze();
			return pattern;
		}

		private Pattern<Word, ShapeNode> LoadPhoneticSequence(XElement pseqElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables, bool createGroups)
		{
			if (pseqElem == null)
				return Pattern<Word, ShapeNode>.New().Value;
			var pattern = new Pattern<Word, ShapeNode>((string) pseqElem.Attribute("id"), LoadPatternNodes(pseqElem, variables, createGroups));
			pattern.Freeze();
			return pattern;
		}

		private Stratum GetStratum(string id)
		{
			Stratum stratum;
			if (!_strata.TryGetValue(id, out stratum))
				throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Stratum '{0}' is unknown.", id));
			return stratum;
		}

		private SymbolTable GetTable(string id)
		{
			SymbolTable table;
			if (!_tables.TryGetValue(id, out table))
				throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Character definition table {0} is unknown.", id));
			return table;
		}

		private string GetStrRep(string id)
		{
			string strRep;
			if (!_repIds.TryGetValue(id, out strRep))
				throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Character definition {0} is unknown.", id));
			return strRep;
		}

		private Feature GetFeature(FeatureSystem featSys, string id)
		{
			Feature feature;
			if (!featSys.TryGetFeature(id, out feature))
				throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Unknown feature '{0}'.", id));
			return feature;
		}

		private FeatureSymbol GetFeatureSymbol(SymbolicFeature feature, string id)
		{
			FeatureSymbol symbol;
			if (!feature.PossibleSymbols.TryGetValue(id, out symbol))
				throw new LoadException(LoadErrorCode.UndefinedObject, string.Format("Unknown value '{0}' for feature '{1}'.", id, feature.ID));
			return symbol;
		}
	}
}
