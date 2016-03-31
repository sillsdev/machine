using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.HermitCrab.MorphologicalRules;
using SIL.Machine.HermitCrab.PhonologicalRules;
using SIL.Machine.Matching;

namespace SIL.Machine.HermitCrab
{
	/// <summary>
	/// This class represents the loader for HC.NET's XML input format.
	/// </summary>
	public class XmlLoader
	{
		private class ResourceXmlResolver : XmlUrlResolver
		{
			public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
			{
				string fileName = Path.GetFileName(absoluteUri.ToString());
				if (fileName == "HermitCrabInput.dtd")
					return GetType().Assembly.GetManifestResourceStream("SIL.Machine.HermitCrab.HermitCrabInput.dtd");
				return base.GetEntity(absoluteUri, role, ofObjectToReturn);
			}

			public override ICredentials Credentials
			{
				set { throw new NotImplementedException(); }
			}
		}

		public static Language Load(string configPath)
		{
			return Load(configPath, null);
		}

		public static Language Load(string configPath, Action<Exception, string> errorHandler)
		{
			var loader = new XmlLoader(configPath, errorHandler);
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

		private SymbolicFeature _posFeature;
		private readonly ComplexFeature _headFeature;
		private readonly ComplexFeature _footFeature;

		private readonly string _configPath;
		private readonly Action<Exception, string> _errorHandler;
		private readonly Dictionary<string, string> _repIds;
		private readonly ShapeSpanFactory _spanFactory;

		private readonly Dictionary<string, SymbolTable> _tables; 
		private readonly Dictionary<string, MprFeature> _mprFeatures;
		private readonly Dictionary<string, FeatureStruct> _natClasses;
		private readonly Dictionary<string, IMorphologicalRule> _mrules;
		private readonly Dictionary<string, AffixTemplate> _templates;
		private readonly HashSet<string> _templateRules; 
		private readonly Dictionary<string, Stratum> _strata;
		private readonly Dictionary<string, LexFamily> _families;
		private readonly Dictionary<string, Morpheme> _morphemes;
		private readonly Dictionary<string, Allomorph> _allomorphs;
		private readonly Dictionary<string, StemName> _stemNames; 

		private XmlLoader(string configPath, Action<Exception, string> errorHandler)
		{
			_configPath = configPath;
			_errorHandler = errorHandler;
			_repIds = new Dictionary<string, string>();
			_spanFactory = new ShapeSpanFactory();

			_headFeature = new ComplexFeature("head") { Description = "Head" };
			_footFeature = new ComplexFeature("foot") { Description = "Foot" };

			_tables = new Dictionary<string, SymbolTable>();
			_mprFeatures = new Dictionary<string, MprFeature>();
			_natClasses = new Dictionary<string, FeatureStruct>();
			_mrules = new Dictionary<string, IMorphologicalRule>();
			_templates = new Dictionary<string, AffixTemplate>();
			_templateRules = new HashSet<string>();
			_strata = new Dictionary<string, Stratum>();
			_families = new Dictionary<string, LexFamily>();
			_morphemes = new Dictionary<string, Morpheme>();
			_allomorphs = new Dictionary<string, Allomorph>();
			_stemNames = new Dictionary<string, StemName>();
		}

		private Language Load()
		{
			var settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Parse,
					ValidationType = Type.GetType("Mono.Runtime") == null ? ValidationType.DTD : ValidationType.None,
					XmlResolver = new ResourceXmlResolver()
				};

			using (XmlReader reader = XmlReader.Create(_configPath, settings))
			{
				XDocument doc = XDocument.Load(reader);
				LoadLanguage(doc.Elements("HermitCrabInput").Elements("Language").Single(IsActive));
				return _language;
			}
		}

		private static bool IsActive(XElement elem)
		{
			return (string) elem.Attribute("isActive") == "yes";
		}

		private void LoadLanguage(XElement langElem)
		{
			_language = new Language { Name = (string) langElem.Element("Name") };

			IEnumerable<FeatureSymbol> posSymbols = langElem.Elements("PartsOfSpeech").Elements("PartOfSpeech")
				.Select(e => new FeatureSymbol((string) e.Attribute("id"), (string) e.Element("Name")));
			_posFeature = new SymbolicFeature("pos", posSymbols) { Description = "POS" };

			_language.SyntacticFeatureSystem.Add(_posFeature);

			foreach (XElement mfElem in langElem.Elements("MorphologicalPhonologicalRuleFeatures").Elements("MorphologicalPhonologicalRuleFeature").Where(IsActive))
				_mprFeatures[(string) mfElem.Attribute("id")] = new MprFeature { Name = (string) mfElem };

			foreach (XElement mprFeatGroupElem in langElem.Elements("MorphologicalPhonologicalRuleFeatures").Elements("MorphologicalPhonologicalRuleFeatureGroup").Where(IsActive))
				LoadMprFeatGroup(mprFeatGroupElem);

			LoadFeatureSystem(langElem.Elements("PhonologicalFeatureSystem").SingleOrDefault(IsActive), _language.PhoneticFeatureSystem);
			_language.PhoneticFeatureSystem.Freeze();

			_language.SyntacticFeatureSystem.Add(_headFeature);
			LoadFeatureSystem(langElem.Element("HeadFeatures"), _language.SyntacticFeatureSystem);
			_language.SyntacticFeatureSystem.Add(_footFeature);
			LoadFeatureSystem(langElem.Element("FootFeatures"), _language.SyntacticFeatureSystem);
			_language.SyntacticFeatureSystem.Freeze();

			foreach (XElement posElem in langElem.Elements("PartsOfSpeech").Elements("PartOfSpeech"))
			{
				foreach (XElement stemNameElem in posElem.Elements("StemNames").Elements("StemName"))
					LoadStemName(stemNameElem, (string) posElem.Attribute("id"));
			}

			foreach (XElement charDefTableElem in langElem.Elements("CharacterDefinitionTable").Where(IsActive))
				LoadSymbolTable(charDefTableElem);

			foreach (XElement natClassElem in langElem.Elements("NaturalClasses").Elements("FeatureNaturalClass").Where(IsActive))
				_natClasses[(string) natClassElem.Attribute("id")] = LoadPhoneticFeatureStruct(natClassElem);

			foreach (XElement natClassElem in langElem.Elements("NaturalClasses").Elements("SegmentNaturalClass").Where(IsActive))
				LoadSegmentNaturalClass(natClassElem);

			foreach (XElement mruleElem in langElem.Elements("MorphologicalRules").Elements().Where(IsActive))
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
					Stratum stratum = _strata[(string) mruleElem.Attribute("stratum")];
					stratum.MorphologicalRules.Add(rule);
				}
			}

			foreach (XElement familyElem in langElem.Elements("Lexicon").Elements("Families").Elements("Family").Where(IsActive))
				_families[(string) familyElem.Attribute("id")] = new LexFamily { Name = (string) familyElem.Element("Name") };

			foreach (XElement entryElem in langElem.Elements("Lexicon").Elements("LexicalEntry").Where(IsActive))
				LoadLexEntry(entryElem);

			// co-occurrence rules cannot be loaded until all of the morphemes and their allomorphs have been loaded
			foreach (XElement morphemeElem in langElem.Elements("Lexicon").Elements("LexicalEntry").Concat(langElem.Elements("MorphologicalRules").Elements()).Where(IsActive))
			{
				var morphemeID = (string) morphemeElem.Attribute("id");
				Morpheme morpheme;
				if (_morphemes.TryGetValue(morphemeID, out morpheme))
				{
					morpheme.RequiredMorphemeCoOccurrences.AddRange(LoadMorphemeCoOccurrenceRules(morphemeElem.Element("RequiredMorphemeCoOccurrences")));
					morpheme.ExcludedMorphemeCoOccurrences.AddRange(LoadMorphemeCoOccurrenceRules(morphemeElem.Element("ExcludedMorphemeCoOccurrences")));
				}

				foreach (XElement alloElem in morphemeElem.Elements("Allomorphs").Elements("Allomorph")
					.Concat(morphemeElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubruleStructure").Where(IsActive)))
				{
					var alloID = (string) alloElem.Attribute("id");
					Allomorph allomorph;
					if (_allomorphs.TryGetValue(alloID, out allomorph))
					{
						allomorph.RequiredAllomorphCoOccurrences.AddRange(LoadAllomorphCoOccurrenceRules(alloElem.Element("RequiredAllomorphCoOccurrences")));
						allomorph.ExcludedAllomorphCoOccurrences.AddRange(LoadAllomorphCoOccurrenceRules(alloElem.Element("ExcludedAllomorphCoOccurrences")));
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
				catch (Exception e)
				{
					if (_errorHandler != null)
						_errorHandler(e, (string) pruleElem.Attribute("id"));
					else
						throw;
				}
			}
		}

		private void LoadStemName(XElement stemNameElem, string posID)
		{
			var regions = new List<FeatureStruct>();
			foreach (XElement regionElem in stemNameElem.Elements("Regions").Elements("Region"))
			{
				FeatureStruct fs = LoadSyntacticFeatureStruct(regionElem);
				fs.AddValue(_posFeature, _posFeature.PossibleSymbols[posID]);
				fs.Freeze();
				regions.Add(fs);
			}

			_stemNames[(string) stemNameElem.Attribute("id")] = new StemName(regions) { Name = (string) stemNameElem.Element("Name") };
		}

		private void LoadMprFeatGroup(XElement mprFeatGroupElem)
		{
			var group = new MprFeatureGroup { Name = (string) mprFeatGroupElem.Element("Name") };
			group.MatchType = GetGroupMatchType((string) mprFeatGroupElem.Attribute("matchType"));
			group.Output = GetGroupOutput((string) mprFeatGroupElem.Attribute("outputType"));
			var mprFeatIdsStr = (string) mprFeatGroupElem.Attribute("features");
			foreach (MprFeature mprFeat in LoadMprFeatures(mprFeatIdsStr))
				group.MprFeatures.Add(mprFeat);
		}

		private void LoadStratum(XElement stratumElem)
		{
			var stratum = new Stratum(_tables[(string) stratumElem.Attribute("characterDefinitionTable")])
			              	{
			              		Name = (string) stratumElem.Element("Name"),
			              		MorphologicalRuleOrder = GetMorphologicalRuleOrder((string) stratumElem.Attribute("morphologicalRuleOrder"))
			              	};

			var tempIDsStr = (string) stratumElem.Attribute("affixTemplates");
			if (!string.IsNullOrEmpty(tempIDsStr))
			{
				foreach (string tempID in tempIDsStr.Split(' '))
				{
					AffixTemplate template = _templates[tempID];
					stratum.AffixTemplates.Add(template);
				}
			}

			_language.Strata.Add(stratum);
			_strata[(string) stratumElem.Attribute("id")] = stratum;
		}

		private void LoadLexEntry(XElement entryElem)
		{
			var entryID = (string) entryElem.Attribute("id");
			var entry = new LexEntry { Gloss = (string) entryElem.Element("Gloss") };

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
				LexFamily family = _families[familyID];
				family.Entries.Add(entry);
			}

			Stratum stratum = _strata[(string) entryElem.Attribute("stratum")];
			foreach (XElement alloElem in entryElem.Elements("Allomorphs").Elements("Allomorph").Where(IsActive))
			{
				try
				{
					RootAllomorph allomorph = LoadRootAllomorph(alloElem, stratum.SymbolTable);
					entry.Allomorphs.Add(allomorph);
					_allomorphs[(string) alloElem.Attribute("id")] = allomorph;
				}
				catch (Exception e)
				{
					if (_errorHandler != null)
						_errorHandler(e, entryID);
					else
						throw;
				}
			}

			if (entry.Allomorphs.Count > 0)
			{
				stratum.Entries.Add(entry);
				_morphemes[entryID] = entry;
			}
		}

		private RootAllomorph LoadRootAllomorph(XElement alloElem, SymbolTable table)
		{
			var shapeStr = (string) alloElem.Element("PhoneticShape");
			Shape shape = table.Segment(shapeStr);
			if (shape.All(n => n.Type() == HCFeatureSystem.Boundary))
				throw new InvalidShapeException(shapeStr, 0);
			var allomorph = new RootAllomorph(shape);

			allomorph.RequiredEnvironments.AddRange(LoadAllomorphEnvironments(alloElem.Element("RequiredEnvironments")));
			allomorph.ExcludedEnvironments.AddRange(LoadAllomorphEnvironments(alloElem.Element("ExcludedEnvironments")));

			var stemNameIDStr = (string) alloElem.Attribute("stemName");
			if (!string.IsNullOrEmpty(stemNameIDStr))
				allomorph.StemName = _stemNames[stemNameIDStr];

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
			var morphemes = new List<Morpheme>();
			foreach (string morphemeID in morphemeIDsStr.Split(' '))
			{
				Morpheme morpheme = _morphemes[morphemeID];
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
			var allomorphs = new List<Allomorph>();
			foreach (string allomorphID in allomorphIDsStr.Split(' '))
			{
				Allomorph allomorph = _allomorphs[allomorphID];
				allomorphs.Add(allomorph);
			}
			return new AllomorphCoOccurrenceRule(allomorphs, adjacency);
		}

		private void LoadProperties(XElement propsElem, IDictionary props)
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
				Feature feature = _language.SyntacticFeatureSystem.GetFeature((string) featValElem.Attribute("feature"));
				var valueIDsStr = (string) featValElem.Attribute("values");
				if (!string.IsNullOrEmpty(valueIDsStr))
				{
					var sf = (SymbolicFeature) feature;
					fs.AddValue(sf, valueIDsStr.Split(' ').Select(id => sf.PossibleSymbols[id]));
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
				IEnumerable<FeatureSymbol> symbols = valueListElem.Elements("Value").Select(e => new FeatureSymbol((string) e.Attribute("id"), (string) e));
				var feature = new SymbolicFeature(id, symbols) { Description = name };
				var defValId = (string) featElem.Attribute("defaultValue");
				if (!string.IsNullOrEmpty(defValId))
					feature.DefaultSymbolID = defValId;
				return feature;
			}

			return new ComplexFeature(id) { Description = name };
		}

		private void LoadSymbolTable(XElement charDefTableElem)
		{
			var table = new SymbolTable(_spanFactory) { Name = (string) charDefTableElem.Element("Name") };
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

			_tables[(string) charDefTableElem.Attribute("id")] = table;
		}

		private void LoadSegmentNaturalClass(XElement natClassElem)
		{
			var id = (string) natClassElem.Attribute("id");
			FeatureStruct fs = null;
			foreach (XElement segElem in natClassElem.Elements("Segment"))
			{
				SymbolTable table = _tables[(string) segElem.Attribute("characterTable")];
				string strRep = _repIds[(string) segElem.Attribute("representation")];
				FeatureStruct segFS = table.GetSymbolFeatureStruct(strRep);
				if (fs == null)
					fs = segFS.Clone();
				else
					fs.Union(segFS);
			}
			if (fs == null)
				fs = new FeatureStruct();
			fs.Freeze();
			_natClasses[id] = fs;
		}

		private FeatureStruct LoadPhoneticFeatureStruct(XElement elem)
		{
			var fs = new FeatureStruct();
			fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
			foreach (XElement featValElem in elem.Elements("FeatureValuePair").Where(IsActive))
			{
				var feature = _language.PhoneticFeatureSystem.GetFeature<SymbolicFeature>((string) featValElem.Attribute("feature"));
				FeatureSymbol symbol = feature.PossibleSymbols[(string) featValElem.Attribute("value")];
				fs.AddValue(feature, new SymbolicFeatureValue(symbol));
			}
			fs.Freeze();
			return fs;
		}

		private void LoadRewriteRule(XElement pruleElem)
		{
			var multAppOrderStr = (string) pruleElem.Attribute("multipleApplicationOrder");
			var prule = new RewriteRule
			            	{
			            		Name = (string) pruleElem.Element("Name"),
			            		ApplicationMode = GetApplicationMode(multAppOrderStr),
								Direction = GetDirection(multAppOrderStr)
			            	};
			Dictionary<string, Tuple<string, SymbolicFeature>> variables = LoadVariables(pruleElem.Element("VariableFeatures"));
			prule.Lhs = LoadPhoneticSequence(pruleElem.Elements("PhoneticInputSequence").Elements("PhoneticSequence").SingleOrDefault(), variables, false);

			foreach (XElement subruleElem in pruleElem.Elements("PhonologicalSubrules").Elements("PhonologicalSubrule"))
				prule.Subrules.Add(LoadRewriteSubrule(subruleElem, variables));

			var stratumIDsStr = (string) pruleElem.Attribute("ruleStrata");
			foreach (string stratumID in stratumIDsStr.Split(' '))
				_strata[stratumID].PhonologicalRules.Add(prule);
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
			var metathesisRule = new MetathesisRule
			                     	{
			                     		Name = (string) metathesisElem.Element("Name"),
										Direction = GetDirection((string) metathesisElem.Attribute("multipleApplicationOrder"))
			                     	};

			var changeIdsStr = (string) metathesisElem.Attribute("structuralChange");
			string[] changeIds = changeIdsStr.Split(' ');
			metathesisRule.LeftGroupName = changeIds[0];
			metathesisRule.RightGroupName = changeIds[1];

			metathesisRule.Pattern = LoadPhoneticTemplate(metathesisElem.Elements("StructuralDescription").Elements("PhoneticTemplate").Single(),
				new Dictionary<string, Tuple<string, SymbolicFeature>>(), true);

			var stratumIDsStr = (string) metathesisElem.Attribute("ruleStrata");
			foreach (string stratumID in stratumIDsStr.Split(' '))
				_strata[stratumID].PhonologicalRules.Add(metathesisRule);
		}

		private void LoadAffixProcessRule(XElement mruleElem)
		{
			var mruleID = (string) mruleElem.Attribute("id");
			var mrule = new AffixProcessRule
			            	{
			            		Name = (string) mruleElem.Element("Name"),
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
					mrule.ObligatorySyntacticFeatures.Add(_language.SyntacticFeatureSystem.GetFeature(obligHeadID));
			}

			var stemNameIDStr = (string) mruleElem.Attribute("requiredStemName");
			if (!string.IsNullOrEmpty(stemNameIDStr))
				mrule.RequiredStemName = _stemNames[stemNameIDStr];

			foreach (XElement subruleElem in mruleElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubruleStructure").Where(IsActive))
			{
				try
				{
					AffixProcessAllomorph allomorph = LoadAffixProcessAllomorph(subruleElem);
					mrule.Allomorphs.Add(allomorph);
					_allomorphs[(string) subruleElem.Attribute("id")] = allomorph;
				}
				catch (Exception e)
				{
					if (_errorHandler != null)
						_errorHandler(e, mruleID);
					else
						throw;
				}
			}

			if (mrule.Allomorphs.Count > 0)
			{
				_mrules[mruleID] = mrule;
				_morphemes[mruleID] = mrule;
			}
		}

		private void LoadRealizationalRule(XElement realRuleElem)
		{
			var realRuleID = (string) realRuleElem.Attribute("id");
			var realRule = new RealizationalAffixProcessRule
							{
								Name = (string) realRuleElem.Element("Name"),
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
					AffixProcessAllomorph allomorph = LoadAffixProcessAllomorph(subruleElem);
					realRule.Allomorphs.Add(allomorph);
					_allomorphs[(string) subruleElem.Attribute("id")] = allomorph;
				}
				catch (Exception e)
				{
					if (_errorHandler != null)
						_errorHandler(e, (string) realRuleElem.Attribute("id"));
					else
						throw;
				}
			}

			if (realRule.Allomorphs.Count > 0)
			{
				_mrules[realRuleID] = realRule;
				_morphemes[realRuleID] = realRule;
			}
		}

		private AffixProcessAllomorph LoadAffixProcessAllomorph(XElement msubruleElem)
		{
			var allomorph = new AffixProcessAllomorph();

			allomorph.RequiredEnvironments.AddRange(LoadAllomorphEnvironments(msubruleElem.Element("RequiredEnvironments")));
			allomorph.ExcludedEnvironments.AddRange(LoadAllomorphEnvironments(msubruleElem.Element("ExcludedEnvironments")));

			var fs = new FeatureStruct();
			XElement requiredHeadFeatElem = msubruleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadSyntacticFeatureStruct(requiredHeadFeatElem));
			XElement requiredFootFeatElem = msubruleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadSyntacticFeatureStruct(requiredFootFeatElem));
			fs.Freeze();
			allomorph.RequiredSyntacticFeatureStruct = fs;

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
			LoadMorphologicalRhs(outputElem.Element("MorphologicalPhoneticOutput"), variables, allomorph.Rhs);

			return allomorph;
		}

		private void LoadMorphologicalLhs(XElement reqPhonInputElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables, IList<Pattern<Word, ShapeNode>> lhs)
		{
			foreach (XElement pseqElem in reqPhonInputElem.Elements("PhoneticSequence"))
				lhs.Add(LoadPhoneticSequence(pseqElem, variables, true));
		}

		private void LoadMorphologicalRhs(XElement phonOutputElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables, IList<MorphologicalOutputAction> rhs)
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
						SymbolTable table = _tables[(string) partElem.Attribute("characterTable")];
						var shapeStr = (string) partElem.Element("PhoneticShape");
						rhs.Add(new InsertShape(table, shapeStr));
						break;
				}
			}
		}

		private void LoadCompoundingRule(XElement compRuleElem)
		{
			var compRuleID = (string) compRuleElem.Attribute("id");
			var compRule = new CompoundingRule
			            	{
			            		Name = (string) compRuleElem.Element("Name"),
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
					compRule.ObligatorySyntacticFeatures.Add(_language.SyntacticFeatureSystem.GetFeature(obligHeadID));
			}

			foreach (XElement subruleElem in compRuleElem.Elements("CompoundSubrules").Elements("CompoundSubruleStructure").Where(IsActive))
			{
				try
				{
					compRule.Subrules.Add(LoadCompoundSubrule(subruleElem));
				}
				catch (Exception e)
				{
					if (_errorHandler != null)
						_errorHandler(e, compRuleID);
					else
						throw;
				}
			}

			if (compRule.Subrules.Count > 0)
				_mrules[compRuleID] = compRule;
		}

		private CompoundingSubrule LoadCompoundSubrule(XElement compSubruleElem)
		{
			var subrule = new CompoundingSubrule();

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

			LoadMorphologicalRhs(outputElem.Element("MorphologicalPhoneticOutput"), variables, subrule.Rhs);

			return subrule;
		}

		private void LoadAffixTemplate(XElement tempElem)
		{
			var template = new AffixTemplate { Name = (string) tempElem.Element("Name"), IsFinal = (bool) tempElem.Attribute("final") };

			var requiredPos = (string) tempElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				template.RequiredSyntacticFeatureStruct = FeatureStruct.New().Feature(_posFeature).EqualTo(ParsePartsOfSpeech(requiredPos)).Value;

			foreach (XElement slotElem in tempElem.Elements("Slot").Where(IsActive))
			{
				var slot = new AffixTemplateSlot { Name = (string) slotElem.Element("Name") };
				var ruleIDsStr = (string) slotElem.Attribute("morphologicalRules");
				IMorphologicalRule lastRule = null;
				foreach (string ruleID in ruleIDsStr.Split(' '))
				{
					IMorphologicalRule rule = _mrules[ruleID];
					slot.Rules.Add(rule);
					_templateRules.Add(ruleID);
					lastRule = rule;
				}

				var optionalStr = (string) slotElem.Attribute("optional");
				var realRule = lastRule as RealizationalAffixProcessRule;
				if (string.IsNullOrEmpty(optionalStr) && realRule != null)
					slot.Optional = !realRule.RealizationalFeatureStruct.IsEmpty;
				else
					slot.Optional = optionalStr == "true";
				template.Slots.Add(slot);
			}

			_templates[(string) tempElem.Attribute("id")] = template;
		}

		private IEnumerable<FeatureSymbol> ParsePartsOfSpeech(string posIdsStr)
		{
			if (!string.IsNullOrEmpty(posIdsStr))
			{
				string[] posIDs = posIdsStr.Split(' ');
				foreach (string posID in posIDs)
					yield return _language.SyntacticFeatureSystem.GetSymbol(posID);
			}
		}

		private IEnumerable<MprFeature> LoadMprFeatures(string mprFeatIDsStr)
		{
			if (string.IsNullOrEmpty(mprFeatIDsStr))
				yield break;

			foreach (string mprFeatID in mprFeatIDsStr.Split(' '))
				yield return _mprFeatures[mprFeatID];
		}

		private Dictionary<string, Tuple<string, SymbolicFeature>> LoadVariables(XElement alphaVarsElem)
		{
			var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();
			if (alphaVarsElem != null)
			{
				foreach (XElement varFeatElem in alphaVarsElem.Elements("VariableFeature"))
				{
					var varName = (string)varFeatElem.Attribute("name");
					var feature = _language.PhoneticFeatureSystem.GetFeature<SymbolicFeature>((string) varFeatElem.Attribute("phonologicalFeature"));
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
						SymbolTable symTable = _tables[(string) recElem.Attribute("characterTable")];
						string strRep = _repIds[(string) recElem.Attribute("representation")];
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
						SymbolTable segsTable = _tables[(string) recElem.Attribute("characterTable")];
						var shapeStr = (string) recElem.Element("PhoneticShape");
						Shape shape = segsTable.Segment(shapeStr);
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
			FeatureStruct fs = _natClasses[natClassID];
			fs = fs.Clone();
			foreach (XElement varElem in ctxtElem.Elements("AlphaVariables").Elements("AlphaVariable"))
			{
				var varID = (string) varElem.Attribute("variableFeature");
				Tuple<string, SymbolicFeature> variable = variables[varID];
				fs.AddValue(variable.Item2, new SymbolicFeatureValue(variable.Item2, variable.Item1, (string)varElem.Attribute("polarity") == "plus"));
			}
			fs.Freeze();
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
	}
}
