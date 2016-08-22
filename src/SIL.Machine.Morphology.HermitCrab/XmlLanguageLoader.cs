using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents the loader for HC.NET's XML input format.
	/// </summary>
	public class XmlLanguageLoader
	{
#if NET4
		private class ResourceXmlResolver : XmlUrlResolver
		{
			public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
			{
				string fileName = System.IO.Path.GetFileName(absoluteUri.ToString());
				if (fileName == "HermitCrabInput.dtd")
					return GetType().Assembly.GetManifestResourceStream("SIL.Machine.Morphology.HermitCrab.HermitCrabInput.dtd");
				return base.GetEntity(absoluteUri, role, ofObjectToReturn);
			}

			public override System.Net.ICredentials Credentials
			{
				set { throw new NotImplementedException(); }
			}
		}
#endif

		public static Language Load(string configPath)
		{
			return Load(configPath, null);
		}

		public static Language Load(string configPath, Action<Exception, string> errorHandler)
		{
			var loader = new XmlLanguageLoader(configPath, errorHandler);
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

			return MorphologicalRuleOrder.Linear;
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

		private static MorphCoOccurrenceAdjacency GetMorphCoOccurrenceAdjacency(string adjacencyStr)
		{
			switch (adjacencyStr)
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

		private static ConstraintType GetConstraintType(string typeStr)
		{
			switch (typeStr)
			{
				case "exclude":
					return ConstraintType.Exclude;

				case "require":
					return ConstraintType.Require;
			}
			return ConstraintType.Exclude;
		}

		private Language _language;

		private SymbolicFeature _posFeature;
		private ComplexFeature _headFeature;
		private ComplexFeature _footFeature;

		private readonly string _configPath;
		private readonly Action<Exception, string> _errorHandler;
		private readonly ShapeSpanFactory _spanFactory;

		private readonly Dictionary<string, CharacterDefinitionTable> _tables;
		private readonly Dictionary<string, CharacterDefinition> _charDefs;
		private readonly Dictionary<string, MprFeature> _mprFeatures;
		private readonly Dictionary<string, NaturalClass> _natClasses;
		private readonly Dictionary<string, LexFamily> _families;
		private readonly Dictionary<string, Morpheme> _morphemes;
		private readonly Dictionary<string, Allomorph> _allomorphs;
		private readonly Dictionary<string, StemName> _stemNames;
		private readonly Dictionary<string, IPhonologicalRule> _prules;

		private XmlLanguageLoader(string configPath, Action<Exception, string> errorHandler)
		{
			_configPath = configPath;
			_errorHandler = errorHandler;
			_spanFactory = new ShapeSpanFactory();

			_tables = new Dictionary<string, CharacterDefinitionTable>();
			_charDefs = new Dictionary<string, CharacterDefinition>();
			_mprFeatures = new Dictionary<string, MprFeature>();
			_natClasses = new Dictionary<string, NaturalClass>();
			_families = new Dictionary<string, LexFamily>();
			_morphemes = new Dictionary<string, Morpheme>();
			_allomorphs = new Dictionary<string, Allomorph>();
			_stemNames = new Dictionary<string, StemName>();
			_prules = new Dictionary<string, IPhonologicalRule>();
		}

		private Language Load()
		{
			var settings = new XmlReaderSettings
			{
#if NET4
				DtdProcessing = DtdProcessing.Parse,
				ValidationType = Type.GetType("Mono.Runtime") == null ? ValidationType.DTD : ValidationType.None,
				XmlResolver = new ResourceXmlResolver()
#elif NET_STD13
				DtdProcessing = DtdProcessing.Ignore
#endif
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
			return ((string) elem.Attribute("isActive") ?? "yes") == "yes";
		}

		private void LoadLanguage(XElement langElem)
		{
			_language = new Language { Name = (string) langElem.Element("Name") };

			IEnumerable<FeatureSymbol> posSymbols = langElem.Elements("PartsOfSpeech").Elements("PartOfSpeech")
				.Select(e => new FeatureSymbol((string) e.Attribute("id"), (string) e.Element("Name")));
			_posFeature = _language.SyntacticFeatureSystem.AddPartsOfSpeech(posSymbols);

			XElement phonFeatSysElem = langElem.Elements("PhonologicalFeatureSystem").SingleOrDefault(IsActive);
			if (phonFeatSysElem != null)
				LoadPhonologicalFeatureSystem(phonFeatSysElem);
			_language.PhonologicalFeatureSystem.Freeze();

			XElement headFeatsElem = langElem.Element("HeadFeatures");
			if (headFeatsElem != null)
			{
				_headFeature = _language.SyntacticFeatureSystem.AddHeadFeature();
				LoadSyntacticFeatureSystem(headFeatsElem, SyntacticFeatureType.Head);
			}
			XElement footFeatsElem = langElem.Element("FootFeatures");
			if (footFeatsElem != null)
			{
				_footFeature = _language.SyntacticFeatureSystem.AddFootFeature();
				LoadSyntacticFeatureSystem(footFeatsElem, SyntacticFeatureType.Foot);
			}
			_language.SyntacticFeatureSystem.Freeze();

			foreach (XElement mfElem in langElem.Elements("MorphologicalPhonologicalRuleFeatures").Elements("MorphologicalPhonologicalRuleFeature").Where(IsActive))
			{
				var mprFeature = new MprFeature {Name = (string) mfElem};
				_language.MprFeatures.Add(mprFeature);
				_mprFeatures[(string) mfElem.Attribute("id")] = mprFeature;
			}

			foreach (XElement mprFeatGroupElem in langElem.Elements("MorphologicalPhonologicalRuleFeatures").Elements("MorphologicalPhonologicalRuleFeatureGroup").Where(IsActive))
				LoadMprFeatGroup(mprFeatGroupElem);

			foreach (XElement stemNameElem in langElem.Elements("StemNames").Elements("StemName"))
				LoadStemName(stemNameElem);

			foreach (XElement charDefTableElem in langElem.Elements("CharacterDefinitionTable").Where(IsActive))
				LoadCharacterDefinitionTable(charDefTableElem);

			foreach (XElement natClassElem in langElem.Elements("NaturalClasses").Elements().Where(IsActive))
				LoadNaturalClass(natClassElem);

			foreach (XElement familyElem in langElem.Elements("Families").Elements("Family").Where(IsActive))
			{
				var family = new LexFamily {Name = (string) familyElem};
				_language.Families.Add(family);
				_families[(string) familyElem.Attribute("id")] = family;
			}

			foreach (XElement pruleElem in langElem.Elements("PhonologicalRuleDefinitions").Elements().Where(IsActive))
				LoadPhonologicalRule(pruleElem);

			foreach (XElement stratumElem in langElem.Elements("Strata").Elements("Stratum").Where(IsActive))
				LoadStratum(stratumElem);

			foreach (XElement coOccurElem in langElem.Elements("MorphemeCoOccurrenceRules").Elements("MorphemeCoOccurrenceRule").Where(IsActive))
				LoadMorphemeCoOccurrenceRule(coOccurElem);

			foreach (XElement coOccurElem in langElem.Elements("AllomorphCoOccurrenceRules").Elements("AllomorphCoOccurrenceRule").Where(IsActive))
				LoadAllomorphCoOccurrenceRule(coOccurElem);
		}

		private void LoadStemName(XElement stemNameElem)
		{
			var posIDs = (string) stemNameElem.Attribute("partsOfSpeech");
			FeatureSymbol[] pos = posIDs.Split(' ').Select(id => _posFeature.PossibleSymbols[id]).ToArray();
			var regions = new List<FeatureStruct>();
			foreach (XElement regionElem in stemNameElem.Elements("Regions").Elements("Region"))
			{
				var fs = new FeatureStruct();
				fs.AddValue(_posFeature, pos);
				XElement headFeatElem = regionElem.Element("AssignedHeadFeatures");
				if (headFeatElem != null)
					fs.AddValue(_headFeature, LoadFeatureStruct(headFeatElem, _language.SyntacticFeatureSystem));
				XElement footFeatElem = regionElem.Element("AssignedFootFeatures");
				if (footFeatElem != null)
					fs.AddValue(_footFeature, LoadFeatureStruct(footFeatElem, _language.SyntacticFeatureSystem));
				fs.Freeze();
				regions.Add(fs);
			}

			var stemName = new StemName(regions) {Name = (string) stemNameElem.Element("Name")};
			_language.StemNames.Add(stemName);
			_stemNames[(string) stemNameElem.Attribute("id")] = stemName;
		}

		private void LoadMprFeatGroup(XElement mprFeatGroupElem)
		{
			var group = new MprFeatureGroup { Name = (string) mprFeatGroupElem.Element("Name") };
			group.MatchType = GetGroupMatchType((string) mprFeatGroupElem.Attribute("matchType"));
			group.Output = GetGroupOutput((string) mprFeatGroupElem.Attribute("outputType"));
			var mprFeatIdsStr = (string) mprFeatGroupElem.Attribute("features");
			foreach (MprFeature mprFeat in LoadMprFeatures(mprFeatIdsStr))
				group.MprFeatures.Add(mprFeat);
			_language.MprFeatureGroups.Add(group);
		}

		private void LoadStratum(XElement stratumElem)
		{
			var stratum = new Stratum(_tables[(string) stratumElem.Attribute("characterDefinitionTable")])
			{
				Name = (string) stratumElem.Element("Name"),
				MorphologicalRuleOrder = GetMorphologicalRuleOrder((string) stratumElem.Attribute("morphologicalRuleOrder"))
			};

			var pruleIdsStr = (string) stratumElem.Attribute("phonologicalRules");
			if (!string.IsNullOrEmpty(pruleIdsStr))
			{
				foreach (string pruleId in pruleIdsStr.Split(' '))
				{
					IPhonologicalRule prule;
					if (_prules.TryGetValue(pruleId, out prule))
						stratum.PhonologicalRules.Add(prule);
				}
			}

			var mrules = new Dictionary<string, IMorphologicalRule>();
			foreach (XElement mruleElem in stratumElem.Elements("MorphologicalRuleDefinitions").Elements().Where(IsActive))
			{
				IMorphologicalRule mrule = null;
				bool loaded = false;
				switch (mruleElem.Name.LocalName)
				{
					case "MorphologicalRule":
						loaded = TryLoadAffixProcessRule(mruleElem, stratum.CharacterDefinitionTable, out mrule);
						break;

					case "RealizationalRule":
						loaded = TryLoadRealizationalRule(mruleElem, stratum.CharacterDefinitionTable, out mrule);
						break;

					case "CompoundingRule":
						loaded = TryLoadCompoundingRule(mruleElem, stratum.CharacterDefinitionTable, out mrule);
						break;
				}

				if (loaded)
					mrules[(string) mruleElem.Attribute("id")] = mrule;
			}

			var mruleIdsStr = (string) stratumElem.Attribute("morphologicalRules");
			if (!string.IsNullOrEmpty(mruleIdsStr))
			{
				foreach (string mruleId in mruleIdsStr.Split(' '))
				{
					IMorphologicalRule mrule;
					if (mrules.TryGetValue(mruleId, out mrule))
						stratum.MorphologicalRules.Add(mrule);
				}
			}

			foreach (XElement tempElem in stratumElem.Elements("AffixTemplates").Elements("AffixTemplate").Where(IsActive))
				stratum.AffixTemplates.Add(LoadAffixTemplate(tempElem, mrules));

			foreach (XElement entryElem in stratumElem.Elements("LexicalEntries").Elements("LexicalEntry").Where(IsActive))
			{
				LexEntry entry;
				if (TryLoadLexEntry(entryElem, stratum.CharacterDefinitionTable, out entry))
					stratum.Entries.Add(entry);
			}

			_language.Strata.Add(stratum);
		}

		private bool TryLoadLexEntry(XElement entryElem, CharacterDefinitionTable table, out LexEntry entry)
		{
			var id = (string) entryElem.Attribute("id");
			entry = new LexEntry
			{
				Id = (string) entryElem.Element("MorphemeId"),
				Gloss = (string) entryElem.Element("Gloss"),
			};

			var fs = new FeatureStruct();
			var pos = (string) entryElem.Attribute("partOfSpeech");
			if (!string.IsNullOrEmpty(pos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(pos));

			XElement headFeatElem = entryElem.Element("AssignedHeadFeatures");
			if (headFeatElem != null)
				fs.AddValue(_headFeature, LoadFeatureStruct(headFeatElem, _language.SyntacticFeatureSystem));
			XElement footFeatElem = entryElem.Element("AssignedFootFeatures");
			if (footFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(footFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			entry.SyntacticFeatureStruct = fs;

			entry.MprFeatures.UnionWith(LoadMprFeatures((string) entryElem.Attribute("ruleFeatures")));

			var familyID = (string) entryElem.Attribute("family");
			if (!string.IsNullOrEmpty(familyID))
			{
				LexFamily family = _families[familyID];
				family.Entries.Add(entry);
			}

			entry.IsPartial = (bool?) entryElem.Attribute("partial") ?? false;

			LoadProperties(entryElem.Element("Properties"), entry.Properties);

			foreach (XElement alloElem in entryElem.Elements("Allomorphs").Elements("Allomorph").Where(IsActive))
			{
				try
				{
					RootAllomorph allomorph = LoadRootAllomorph(alloElem, table);
					entry.Allomorphs.Add(allomorph);
					_allomorphs[(string) alloElem.Attribute("id")] = allomorph;
				}
				catch (Exception e)
				{
					if (_errorHandler != null)
						_errorHandler(e, id);
					else
						throw;
				}
			}

			if (entry.Allomorphs.Count > 0)
			{
				_morphemes[id] = entry;
				return true;
			}

			entry = null;
			return false;
		}

		private RootAllomorph LoadRootAllomorph(XElement alloElem, CharacterDefinitionTable table)
		{
			var shapeStr = (string) alloElem.Element("PhoneticShape");
			Segments segments = new Segments(table, shapeStr);
			if (segments.Shape.All(n => n.Type() == HCFeatureSystem.Boundary))
				throw new InvalidShapeException(shapeStr, 0);
			var allomorph = new RootAllomorph(segments)
			{
				IsBound = (bool?) alloElem.Attribute("isBound") ?? false
			};

			allomorph.Environments.AddRange(LoadAllomorphEnvironments(alloElem.Element("RequiredEnvironments"), ConstraintType.Require, table));
			allomorph.Environments.AddRange(LoadAllomorphEnvironments(alloElem.Element("ExcludedEnvironments"), ConstraintType.Exclude, table));

			var stemNameIDStr = (string) alloElem.Attribute("stemName");
			if (!string.IsNullOrEmpty(stemNameIDStr))
				allomorph.StemName = _stemNames[stemNameIDStr];

			LoadProperties(alloElem.Element("Properties"), allomorph.Properties);

			return allomorph;
		}

		private IEnumerable<AllomorphEnvironment> LoadAllomorphEnvironments(XElement envsElem, ConstraintType type, CharacterDefinitionTable defaultTable)
		{
			if (envsElem == null)
				yield break;

			foreach (XElement envElem in envsElem.Elements("Environment"))
				yield return LoadAllomorphEnvironment(envElem, type, defaultTable);
		}

		private AllomorphEnvironment LoadAllomorphEnvironment(XElement envElem, ConstraintType type, CharacterDefinitionTable defaultTable)
		{
			var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();

			Pattern<Word, ShapeNode> leftEnv = LoadPhoneticTemplate(envElem.Elements("LeftEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables, defaultTable);
			Pattern<Word, ShapeNode> rightEnv = LoadPhoneticTemplate(envElem.Elements("RightEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables, defaultTable);
			return new AllomorphEnvironment(_spanFactory, type, leftEnv, rightEnv);
		}

		private void LoadMorphemeCoOccurrenceRule(XElement coOccurElem)
		{
			ConstraintType type = GetConstraintType((string) coOccurElem.Attribute("type"));
			Morpheme primaryMorpheme = _morphemes[(string) coOccurElem.Attribute("primaryMorpheme")];
			MorphCoOccurrenceAdjacency adjacency = GetMorphCoOccurrenceAdjacency((string) coOccurElem.Attribute("adjacency"));
			var morphemeIDsStr = (string) coOccurElem.Attribute("otherMorphemes");
			var rule = new MorphemeCoOccurrenceRule(type, morphemeIDsStr.Split(' ').Select(id => _morphemes[id]), adjacency);
			primaryMorpheme.MorphemeCoOccurrenceRules.Add(rule);
			_language.MorphemeCoOccurrenceRules.Add(rule);
		}

		private void LoadAllomorphCoOccurrenceRule(XElement coOccurElem)
		{
			ConstraintType type = GetConstraintType((string) coOccurElem.Attribute("type"));
			Allomorph primaryAllomorph = _allomorphs[(string) coOccurElem.Attribute("primaryAllomorph")];
			MorphCoOccurrenceAdjacency adjacency = GetMorphCoOccurrenceAdjacency((string) coOccurElem.Attribute("adjacency"));
			var allomorphIDsStr = (string) coOccurElem.Attribute("otherAllomorphs");
			var rule = new AllomorphCoOccurrenceRule(type, allomorphIDsStr.Split(' ').Select(id => _allomorphs[id]), adjacency);
			primaryAllomorph.AllomorphCoOccurrenceRules.Add(rule);
			_language.AllomorphCoOccurrenceRules.Add(rule);
		}

		private void LoadProperties(XElement propsElem, IDictionary<string, object> props)
		{
			if (propsElem == null)
				return;

			foreach (XElement propElem in propsElem.Elements("Property"))
				props[(string) propElem.Attribute("name")] = (string) propElem;
		}

		private FeatureStruct LoadFeatureStruct(XElement elem, FeatureSystem featSys)
		{
			var fs = new FeatureStruct();
			foreach (XElement featValElem in elem.Elements("FeatureValue").Where(IsActive))
			{
				Feature feature = featSys.GetFeature((string) featValElem.Attribute("feature"));
				var valueIDsStr = (string) featValElem.Attribute("symbolValues");
				if (!string.IsNullOrEmpty(valueIDsStr))
				{
					var sf = (SymbolicFeature) feature;
					fs.AddValue(sf, valueIDsStr.Split(' ').Select(id => sf.PossibleSymbols[id]));
				}
				else
				{
					var cf = (ComplexFeature) feature;
					fs.AddValue(cf, LoadFeatureStruct(featValElem, featSys));
				}
			}
			return fs;
		}

		private void LoadPhonologicalFeatureSystem(XElement featSysElem)
		{
			foreach (XElement featDefElem in featSysElem.Elements("SymbolicFeature").Where(IsActive))
				_language.PhonologicalFeatureSystem.Add(LoadFeature(featDefElem));
		}

		private void LoadSyntacticFeatureSystem(XElement featSysElem, SyntacticFeatureType type)
		{
			foreach (XElement featDefElem in featSysElem.Elements().Where(IsActive))
				_language.SyntacticFeatureSystem.Add(LoadFeature(featDefElem), type);
		}

		private Feature LoadFeature(XElement featElem)
		{
			var id = (string) featElem.Attribute("id");
			var name = (string) featElem.Element("Name");
			switch (featElem.Name.LocalName)
			{
				case "SymbolicFeature":
					IEnumerable<FeatureSymbol> symbols = featElem.Elements("Symbols").Elements("Symbol").Select(e => new FeatureSymbol((string) e.Attribute("id"), (string) e));
					var feature = new SymbolicFeature(id, symbols) { Description = name };
					var defValId = (string) featElem.Attribute("defaultSymbol");
					if (!string.IsNullOrEmpty(defValId))
						feature.DefaultSymbolID = defValId;
					return feature;

				case "ComplexFeature":
					return new ComplexFeature(id) {Description = name};
			}

			return null;
		}

		private void LoadCharacterDefinitionTable(XElement charDefTableElem)
		{
			var table = new CharacterDefinitionTable(_spanFactory) { Name = (string) charDefTableElem.Element("Name") };
			foreach (XElement segDefElem in charDefTableElem.Elements("SegmentDefinitions").Elements("SegmentDefinition").Where(IsActive))
			{
				IEnumerable<string> reps = segDefElem.Elements("Representations").Elements("Representation").Select(e => (string) e);
				FeatureStruct fs = null;
				if (_language.PhonologicalFeatureSystem.Count > 0)
					fs = LoadFeatureStruct(segDefElem, _language.PhonologicalFeatureSystem);
				CharacterDefinition cd = table.AddSegment(reps, fs);
				_charDefs[(string) segDefElem.Attribute("id")] = cd;
			}

			foreach (XElement bdryDefElem in charDefTableElem.Elements("BoundaryDefinitions").Elements("BoundaryDefinition").Where(IsActive))
			{
				IEnumerable<string> reps = bdryDefElem.Elements("Representations").Elements("Representation").Select(e => (string) e);
				CharacterDefinition cd = table.AddBoundary(reps);
				_charDefs[(string) bdryDefElem.Attribute("id")] = cd;
			}

			_language.CharacterDefinitionTables.Add(table);
			_tables[(string) charDefTableElem.Attribute("id")] = table;
		}

		private void LoadNaturalClass(XElement natClassElem)
		{
			NaturalClass nc = null;
			switch (natClassElem.Name.LocalName)
			{
				case "FeatureNaturalClass":
					nc = new NaturalClass(LoadFeatureStruct(natClassElem, _language.PhonologicalFeatureSystem))
					{
						Name = (string) natClassElem.Element("Name")
					};
					break;

				case "SegmentNaturalClass":
					nc = new SegmentNaturalClass(natClassElem.Elements("Segment").Select(se => _charDefs[(string) se.Attribute("segment")]))
					{
						Name = (string) natClassElem.Element("Name")
					};
					break;
			}

			_language.NaturalClasses.Add(nc);
			_natClasses[(string) natClassElem.Attribute("id")] = nc;
		}

		private void LoadPhonologicalRule(XElement pruleElem)
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
			prule.Lhs = LoadPhoneticSequence(pruleElem.Elements("PhoneticInput").Elements("PhoneticSequence").SingleOrDefault(), variables);

			foreach (XElement subruleElem in pruleElem.Elements("PhonologicalSubrules").Elements("PhonologicalSubrule").Where(IsActive))
				prule.Subrules.Add(LoadRewriteSubrule(subruleElem, variables));

			_language.PhonologicalRules.Add(prule);
			_prules[(string) pruleElem.Attribute("id")] = prule;
		}

		private RewriteSubrule LoadRewriteSubrule(XElement psubruleElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables)
		{
			var subrule = new RewriteSubrule();

			var requiredPos = (string) psubruleElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				subrule.RequiredSyntacticFeatureStruct = FeatureStruct.New().Feature(_posFeature).EqualTo(ParsePartsOfSpeech(requiredPos)).Value;

			subrule.RequiredMprFeatures.UnionWith(LoadMprFeatures((string) psubruleElem.Attribute("requiredMPRFeatures")));
			subrule.ExcludedMprFeatures.UnionWith(LoadMprFeatures((string) psubruleElem.Attribute("excludedMPRFeatures")));

			subrule.Rhs = LoadPhoneticSequence(psubruleElem.Elements("PhoneticOutput").Elements("PhoneticSequence").SingleOrDefault(), variables);

			XElement envElem = psubruleElem.Element("Environment");
			if (envElem != null)
			{
				subrule.LeftEnvironment = LoadPhoneticTemplate(envElem.Elements("LeftEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables);
				subrule.RightEnvironment = LoadPhoneticTemplate(envElem.Elements("RightEnvironment").Elements("PhoneticTemplate").SingleOrDefault(), variables);
			}

			return subrule;
		}

		private void LoadMetathesisRule(XElement metathesisElem)
		{
			var metathesisRule = new MetathesisRule
			{
				Name = (string) metathesisElem.Element("Name"),
				Direction = GetDirection((string) metathesisElem.Attribute("multipleApplicationOrder")),
				LeftSwitchName = "r",
				RightSwitchName = "l"
			};

			var groupNames = new Dictionary<string, string>
			{
				{(string) metathesisElem.Attribute("leftSwitch"), "r"},
				{(string) metathesisElem.Attribute("rightSwitch"), "l"}
			};
			metathesisRule.Pattern = LoadPhoneticTemplate(metathesisElem.Elements("StructuralDescription").Elements("PhoneticTemplate").Single(),
				new Dictionary<string, Tuple<string, SymbolicFeature>>(), null, groupNames);

			_language.PhonologicalRules.Add(metathesisRule);
			_prules[(string) metathesisElem.Attribute("id")] = metathesisRule;
		}

		private bool TryLoadAffixProcessRule(XElement mruleElem, CharacterDefinitionTable defaultTable, out IMorphologicalRule mrule)
		{
			var id = (string) mruleElem.Attribute("id");
			var affixProcessRule = new AffixProcessRule
			{
				Name = (string) mruleElem.Element("Name"),
				Id = (string) mruleElem.Element("MorphemeId"),
				Gloss = (string) mruleElem.Element("Gloss"),
				Blockable = (bool?) mruleElem.Attribute("blockable") ?? true,
				IsPartial = (bool?) mruleElem.Attribute("partial") ?? false
			};
			var multApp = (string) mruleElem.Attribute("multipleApplication");
			if (!string.IsNullOrEmpty(multApp))
				affixProcessRule.MaxApplicationCount = int.Parse(multApp);

			var fs = new FeatureStruct();
			var requiredPos = (string) mruleElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(requiredPos));
			XElement requiredHeadFeatElem = mruleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadFeatureStruct(requiredHeadFeatElem, _language.SyntacticFeatureSystem));
			XElement requiredFootFeatElem = mruleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(requiredFootFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			affixProcessRule.RequiredSyntacticFeatureStruct = fs;

			fs = new FeatureStruct();
			var outPos = (string) mruleElem.Attribute("outputPartOfSpeech");
			if (!string.IsNullOrEmpty(outPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(outPos));
			XElement outHeadFeatElem = mruleElem.Element("OutputHeadFeatures");
			if (outHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadFeatureStruct(outHeadFeatElem, _language.SyntacticFeatureSystem));
			XElement outFootFeatElem = mruleElem.Element("OutputFootFeatures");
			if (outFootFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(outFootFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			affixProcessRule.OutSyntacticFeatureStruct = fs;

			var obligHeadIDsStr = (string) mruleElem.Attribute("outputObligatoryFeatures");
			if (!string.IsNullOrEmpty(obligHeadIDsStr))
			{
				foreach (string obligHeadID in obligHeadIDsStr.Split(' '))
					affixProcessRule.ObligatorySyntacticFeatures.Add(_language.SyntacticFeatureSystem.GetFeature(obligHeadID));
			}

			var stemNameIDStr = (string) mruleElem.Attribute("requiredStemName");
			if (!string.IsNullOrEmpty(stemNameIDStr))
				affixProcessRule.RequiredStemName = _stemNames[stemNameIDStr];

			LoadProperties(mruleElem.Element("Properties"), affixProcessRule.Properties);

			foreach (XElement subruleElem in mruleElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubrule").Where(IsActive))
			{
				try
				{
					AffixProcessAllomorph allomorph = LoadAffixProcessAllomorph(subruleElem, defaultTable);
					affixProcessRule.Allomorphs.Add(allomorph);
					_allomorphs[(string) subruleElem.Attribute("id")] = allomorph;
				}
				catch (Exception e)
				{
					if (_errorHandler != null)
						_errorHandler(e, id);
					else
						throw;
				}
			}

			if (affixProcessRule.Allomorphs.Count > 0)
			{
				_morphemes[id] = affixProcessRule;
				mrule = affixProcessRule;
				return true;
			}

			mrule = null;
			return false;
		}

		private bool TryLoadRealizationalRule(XElement realRuleElem, CharacterDefinitionTable defaultTable, out IMorphologicalRule mrule)
		{
			var realRuleID = (string) realRuleElem.Attribute("id");
			var realRule = new RealizationalAffixProcessRule
			{
				Name = (string) realRuleElem.Element("Name"),
				Id = (string) realRuleElem.Element("MorphemeId"),
				Gloss = (string) realRuleElem.Element("Gloss"),
				Blockable = (bool?) realRuleElem.Attribute("blockable") ?? true
			};

			var fs = new FeatureStruct();
			XElement requiredHeadFeatElem = realRuleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadFeatureStruct(requiredHeadFeatElem, _language.SyntacticFeatureSystem));
			XElement requiredFootFeatElem = realRuleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(requiredFootFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			realRule.RequiredSyntacticFeatureStruct = fs;

			XElement realFeatElem = realRuleElem.Element("RealizationalFeatures");
			if (realFeatElem != null)
				realRule.RealizationalFeatureStruct = FeatureStruct.New().Feature(_headFeature).EqualTo(LoadFeatureStruct(realFeatElem, _language.SyntacticFeatureSystem)).Value;
			LoadProperties(realRuleElem.Element("Properties"), realRule.Properties);

			foreach (XElement subruleElem in realRuleElem.Elements("MorphologicalSubrules").Elements("MorphologicalSubrule").Where(IsActive))
			{
				try
				{
					AffixProcessAllomorph allomorph = LoadAffixProcessAllomorph(subruleElem, defaultTable);
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
				_morphemes[realRuleID] = realRule;
				mrule = realRule;
				return true;
			}

			mrule = null;
			return false;
		}

		private AffixProcessAllomorph LoadAffixProcessAllomorph(XElement msubruleElem, CharacterDefinitionTable defaultTable)
		{
			var allomorph = new AffixProcessAllomorph();

			allomorph.Environments.AddRange(LoadAllomorphEnvironments(msubruleElem.Element("RequiredEnvironments"), ConstraintType.Require, defaultTable));
			allomorph.Environments.AddRange(LoadAllomorphEnvironments(msubruleElem.Element("ExcludedEnvironments"), ConstraintType.Exclude, defaultTable));

			var fs = new FeatureStruct();
			XElement requiredHeadFeatElem = msubruleElem.Element("RequiredHeadFeatures");
			if (requiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadFeatureStruct(requiredHeadFeatElem, _language.SyntacticFeatureSystem));
			XElement requiredFootFeatElem = msubruleElem.Element("RequiredFootFeatures");
			if (requiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(requiredFootFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			allomorph.RequiredSyntacticFeatureStruct = fs;

			LoadProperties(msubruleElem.Element("Properties"), allomorph.Properties);

			Dictionary<string, Tuple<string, SymbolicFeature>> variables = LoadVariables(msubruleElem.Element("VariableFeatures"));

			XElement inputElem = msubruleElem.Element("MorphologicalInput");
			Debug.Assert(inputElem != null);

			allomorph.RequiredMprFeatures.UnionWith(LoadMprFeatures((string) inputElem.Attribute("requiredMPRFeatures")));
			allomorph.ExcludedMprFeatures.UnionWith(LoadMprFeatures((string) inputElem.Attribute("excludedMPRFeatures")));

			var partNames = new Dictionary<string, string>();
			LoadMorphologicalLhs(inputElem, variables, partNames, allomorph.Lhs, defaultTable);

			XElement outputElem = msubruleElem.Element("MorphologicalOutput");
			Debug.Assert(outputElem != null);

			allomorph.OutMprFeatures.UnionWith(LoadMprFeatures((string) outputElem.Attribute("MPRFeatures")));
			allomorph.ReduplicationHint = GetReduplicationHint((string) outputElem.Attribute("redupMorphType"));
			LoadMorphologicalRhs(outputElem, variables, partNames, allomorph.Rhs, defaultTable);

			return allomorph;
		}

		private void LoadMorphologicalLhs(XElement reqPhonInputElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables,
			Dictionary<string, string> partNames, IList<Pattern<Word, ShapeNode>> lhs, CharacterDefinitionTable defaultTable, string partNamePrefix = null)
		{
			int i = 1;
			foreach (XElement pseqElem in reqPhonInputElem.Elements("PhoneticSequence"))
			{
				var id = (string) pseqElem.Attribute("id");
				string name = null;
				if (!string.IsNullOrEmpty(id))
				{
					name = (partNamePrefix ?? "") + i;
					partNames[id] = name;
				}
				lhs.Add(LoadPhoneticSequence(pseqElem, variables, defaultTable, name));
				i++;
			}
		}

		private void LoadMorphologicalRhs(XElement phonOutputElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables,
			Dictionary<string, string> partNames, IList<MorphologicalOutputAction> rhs, CharacterDefinitionTable defaultTable)
		{
			foreach (XElement partElem in phonOutputElem.Elements())
			{
				switch (partElem.Name.LocalName)
				{
					case "CopyFromInput":
						rhs.Add(new CopyFromInput(partNames[(string) partElem.Attribute("index")]));
						break;

					case "InsertSimpleContext":
						rhs.Add(new InsertSimpleContext(LoadSimpleContext(partElem.Element("SimpleContext"), variables)));
						break;

					case "ModifyFromInput":
						rhs.Add(new ModifyFromInput(partNames[(string) partElem.Attribute("index")], LoadSimpleContext(partElem.Element("SimpleContext"), variables)));
						break;

					case "InsertSegments":
						string tableId = (string) partElem.Attribute("characterDefinitionTable");
						CharacterDefinitionTable table = tableId == null ? defaultTable : _tables[tableId];
						var shapeStr = (string) partElem.Element("PhoneticShape");
						rhs.Add(new InsertSegments(new Segments(table, shapeStr)));
						break;
				}
			}
		}

		private bool TryLoadCompoundingRule(XElement compRuleElem, CharacterDefinitionTable defaultTable, out IMorphologicalRule mrule)
		{
			var compRuleID = (string) compRuleElem.Attribute("id");
			var compRule = new CompoundingRule
			{
				Name = (string) compRuleElem.Element("Name"),
				Blockable = (bool?) compRuleElem.Attribute("blockable") ?? true
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
				fs.AddValue(_headFeature, LoadFeatureStruct(headRequiredHeadFeatElem, _language.SyntacticFeatureSystem));
			XElement headRequiredFootFeatElem = compRuleElem.Element("HeadRequiredFootFeatures");
			if (headRequiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(headRequiredFootFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			compRule.HeadRequiredSyntacticFeatureStruct = fs;

			fs = new FeatureStruct();
			var nonHeadRequiredPos = (string) compRuleElem.Attribute("nonHeadPartsOfSpeech");
			if (!string.IsNullOrEmpty(nonHeadRequiredPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(nonHeadRequiredPos));
			XElement nonHeadRequiredHeadFeatElem = compRuleElem.Element("NonHeadRequiredHeadFeatures");
			if (nonHeadRequiredHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadFeatureStruct(nonHeadRequiredHeadFeatElem, _language.SyntacticFeatureSystem));
			XElement nonHeadRequiredFootFeatElem = compRuleElem.Element("NonHeadRequiredFootFeatures");
			if (nonHeadRequiredFootFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(nonHeadRequiredFootFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			compRule.NonHeadRequiredSyntacticFeatureStruct = fs;

			fs = new FeatureStruct();
			var outPos = (string) compRuleElem.Attribute("outputPartOfSpeech");
			if (!string.IsNullOrEmpty(outPos))
				fs.AddValue(_posFeature, ParsePartsOfSpeech(outPos));
			XElement outHeadFeatElem = compRuleElem.Element("OutputHeadFeatures");
			if (outHeadFeatElem != null)
				fs.AddValue(_headFeature, LoadFeatureStruct(outHeadFeatElem, _language.SyntacticFeatureSystem));
			XElement outFootFeatElem = compRuleElem.Element("OutputFootFeatures");
			if (outFootFeatElem != null)
				fs.AddValue(_footFeature, LoadFeatureStruct(outFootFeatElem, _language.SyntacticFeatureSystem));
			fs.Freeze();
			compRule.OutSyntacticFeatureStruct = fs;

			var obligHeadIDsStr = (string) compRuleElem.Attribute("outputObligatoryFeatures");
			if (!string.IsNullOrEmpty(obligHeadIDsStr))
			{
				foreach (string obligHeadID in obligHeadIDsStr.Split(' '))
					compRule.ObligatorySyntacticFeatures.Add(_language.SyntacticFeatureSystem.GetFeature(obligHeadID));
			}

			foreach (XElement subruleElem in compRuleElem.Elements("CompoundingSubrules").Elements("CompoundingSubrule").Where(IsActive))
			{
				try
				{
					compRule.Subrules.Add(LoadCompoundingSubrule(subruleElem, defaultTable));
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
			{
				mrule = compRule;
				return true;
			}

			mrule = null;
			return false;
		}

		private CompoundingSubrule LoadCompoundingSubrule(XElement compSubruleElem, CharacterDefinitionTable defaultTable)
		{
			var subrule = new CompoundingSubrule();

			Dictionary<string, Tuple<string, SymbolicFeature>> variables = LoadVariables(compSubruleElem.Element("VariableFeatures"));

			XElement headElem = compSubruleElem.Element("HeadMorphologicalInput");
			Debug.Assert(headElem != null);

			subrule.RequiredMprFeatures.UnionWith(LoadMprFeatures((string) headElem.Attribute("requiredMPRFeatures")));
			subrule.ExcludedMprFeatures.UnionWith(LoadMprFeatures((string) headElem.Attribute("excludedMPRFeatures")));

			var partNames = new Dictionary<string, string>();
			LoadMorphologicalLhs(headElem, variables, partNames, subrule.HeadLhs, defaultTable, "head_");

			XElement nonHeadElem = compSubruleElem.Element("NonHeadMorphologicalInput");
			Debug.Assert(nonHeadElem != null);

			LoadMorphologicalLhs(nonHeadElem, variables, partNames, subrule.NonHeadLhs, defaultTable, "nonhead_");

			XElement outputElem = compSubruleElem.Element("MorphologicalOutput");
			Debug.Assert(outputElem != null);

			subrule.OutMprFeatures.UnionWith(LoadMprFeatures((string) outputElem.Attribute("MPRFeatures")));

			LoadMorphologicalRhs(outputElem, variables, partNames, subrule.Rhs, defaultTable);

			return subrule;
		}

		private AffixTemplate LoadAffixTemplate(XElement tempElem, Dictionary<string, IMorphologicalRule> mrules)
		{
			var template = new AffixTemplate {Name = (string) tempElem.Element("Name"), IsFinal = (bool) tempElem.Attribute("final")};

			var requiredPos = (string) tempElem.Attribute("requiredPartsOfSpeech");
			if (!string.IsNullOrEmpty(requiredPos))
				template.RequiredSyntacticFeatureStruct = FeatureStruct.New().Feature(_posFeature).EqualTo(ParsePartsOfSpeech(requiredPos)).Value;

			foreach (XElement slotElem in tempElem.Elements("Slot").Where(IsActive))
			{
				var rules = new List<MorphemicMorphologicalRule>();
				var ruleIDsStr = (string) slotElem.Attribute("morphologicalRules");
				IMorphologicalRule lastRule = null;
				foreach (string ruleID in ruleIDsStr.Split(' '))
				{
					IMorphologicalRule rule;
					if (mrules.TryGetValue(ruleID, out rule))
					{
						rules.Add((MorphemicMorphologicalRule) rule);
						lastRule = rule;
					}
				}
				var slot = new AffixTemplateSlot(rules) {Name = (string) slotElem.Element("Name")};

				var optionalStr = (string) slotElem.Attribute("optional") ?? "false";
				var realRule = lastRule as RealizationalAffixProcessRule;
				if (string.IsNullOrEmpty(optionalStr) && realRule != null)
					slot.Optional = !realRule.RealizationalFeatureStruct.IsEmpty;
				else
					slot.Optional = optionalStr == "true";
				template.Slots.Add(slot);
			}

			return template;
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
					var feature = _language.PhonologicalFeatureSystem.GetFeature<SymbolicFeature>((string) varFeatElem.Attribute("phonologicalFeature"));
					variables[(string)varFeatElem.Attribute("id")] = Tuple.Create(varName, feature);
				}
			}
			return variables;
		}

		private IEnumerable<PatternNode<Word, ShapeNode>> LoadPatternNodes(XElement pseqElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables,
			CharacterDefinitionTable defaultTable, Dictionary<string, string> groupNames)
		{
			foreach (XElement recElem in pseqElem.Elements())
			{
				PatternNode<Word, ShapeNode> node = null;
				switch (recElem.Name.LocalName)
				{
					case "SimpleContext":
						SimpleContext simpleCtxt = LoadSimpleContext(recElem, variables);
						node = new Constraint<Word, ShapeNode>(simpleCtxt.FeatureStruct) {Tag = simpleCtxt};
						break;

					case "Segment":
					case "BoundaryMarker":
						CharacterDefinition cd = _charDefs[(string) recElem.Attribute(recElem.Name.LocalName == "Segment" ? "segment" : "boundary")];
						node = new Constraint<Word, ShapeNode>(cd.FeatureStruct) {Tag = cd};
						break;

					case "OptionalSegmentSequence":
						var minStr = (string) recElem.Attribute("min");
						int min = string.IsNullOrEmpty(minStr) ? 0 : int.Parse(minStr);
						var maxStr = (string) recElem.Attribute("max");
						int max = string.IsNullOrEmpty(maxStr) ? -1 : int.Parse(maxStr);
						node = new Quantifier<Word, ShapeNode>(min, max, new Group<Word, ShapeNode>(LoadPatternNodes(recElem, variables, defaultTable, groupNames)));
						break;

					case "Segments":
						CharacterDefinitionTable segsTable = GetTable(recElem, defaultTable);
						var shapeStr = (string) recElem.Element("PhoneticShape");
						var segments = new Segments(segsTable, shapeStr);
						node = new Group<Word, ShapeNode>(segments.Shape.Select(n => new Constraint<Word, ShapeNode>(n.Annotation.FeatureStruct))) {Tag = segments};
						break;
				}

				Debug.Assert(node != null);
				var id = (string) recElem.Attribute("id");
				string groupName;
				if (groupNames == null || string.IsNullOrEmpty(id) || !groupNames.TryGetValue(id, out groupName))
					yield return node;
				else
					yield return new Group<Word, ShapeNode>(groupName, node);
			}
		}

		private SimpleContext LoadSimpleContext(XElement ctxtElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables)
		{
			var natClassID = (string) ctxtElem.Attribute("naturalClass");
			NaturalClass nc = _natClasses[natClassID];
			var ctxtVars = new List<SymbolicFeatureValue>();
			foreach (XElement varElem in ctxtElem.Elements("AlphaVariables").Elements("AlphaVariable"))
			{
				var varID = (string) varElem.Attribute("variableFeature");
				Tuple<string, SymbolicFeature> variable = variables[varID];
				ctxtVars.Add(new SymbolicFeatureValue(variable.Item2, variable.Item1, ((string) varElem.Attribute("polarity") ?? "plus") == "plus"));
			}
			return new SimpleContext(nc, ctxtVars);
		}

		private Pattern<Word, ShapeNode> LoadPhoneticTemplate(XElement ptempElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables,
			CharacterDefinitionTable defaultTable = null, Dictionary<string, string> groupNames = null)
		{
			var pattern = new Pattern<Word, ShapeNode>();
			if (ptempElem != null)
			{
				if ((string) ptempElem.Attribute("initialBoundaryCondition") == "true")
					pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
				foreach (PatternNode<Word, ShapeNode> node in LoadPatternNodes(ptempElem.Element("PhoneticSequence"), variables, defaultTable, groupNames))
					pattern.Children.Add(node);
				if ((string) ptempElem.Attribute("finalBoundaryCondition") == "true")
					pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
			}
			pattern.Freeze();
			return pattern;
		}

		private Pattern<Word, ShapeNode> LoadPhoneticSequence(XElement pseqElem, Dictionary<string, Tuple<string, SymbolicFeature>> variables,
			CharacterDefinitionTable defaultTable = null, string name = null)
		{
			if (pseqElem == null)
				return Pattern<Word, ShapeNode>.New().Value;
			var pattern = new Pattern<Word, ShapeNode>(name, LoadPatternNodes(pseqElem, variables, defaultTable, null));
			pattern.Freeze();
			return pattern;
		}

		private CharacterDefinitionTable GetTable(XElement elem, CharacterDefinitionTable defaultTable)
		{
			string tableId = (string) elem.Attribute("characterDefinitionTable");
			return tableId == null ? defaultTable : _tables[tableId];
		}
	}
}
