﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class XmlLanguageWriter
    {
        public static void Save(Language language, string configPath)
        {
            using (var writer = new StreamWriter(File.Open(configPath, FileMode.Create)))
            {
                var langWriter = new XmlLanguageWriter(language);
                langWriter.Save(writer);
            }
        }

        private static string GetMorphCoOccurrenceAdjacencyStr(MorphCoOccurrenceAdjacency adjacency)
        {
            switch (adjacency)
            {
                case MorphCoOccurrenceAdjacency.Anywhere:
                    return "anywhere";

                case MorphCoOccurrenceAdjacency.SomewhereToLeft:
                    return "somewhereToLeft";

                case MorphCoOccurrenceAdjacency.SomewhereToRight:
                    return "somewhereToRight";

                case MorphCoOccurrenceAdjacency.AdjacentToLeft:
                    return "adjacentToLeft";

                case MorphCoOccurrenceAdjacency.AdjacentToRight:
                    return "adjacentToRight";
            }

            return null;
        }

        private static string GetConstraintTypeStr(ConstraintType type)
        {
            switch (type)
            {
                case ConstraintType.Exclude:
                    return "exclude";

                case ConstraintType.Require:
                    return "require";
            }

            return null;
        }

        private static XAttribute WriteIDs<T>(string name, IEnumerable<T> items, IDictionary<T, string> ids)
        {
            return new XAttribute(name, string.Join(" ", items.Select(i => ids[i])));
        }

        private static void WritePartsOfSpeechIfPresent(XElement elem, string name, FeatureStruct fs)
        {
            string posStr = string.Join(" ", fs.PartsOfSpeech().Select(v => Normalize(v.ID)));
            if (!string.IsNullOrEmpty(posStr))
                elem.Add(new XAttribute(name, posStr));
        }

        private static string Normalize(string str)
        {
            if (str == null)
                return null;
            return str.Normalize();
        }

        private readonly Language _language;

        private int _nextTableIndex = 1;
        private readonly Dictionary<CharacterDefinitionTable, string> _tables;
        private int _nextMprFeatureIndex = 1;
        private readonly Dictionary<MprFeature, string> _mprFeatures;
        private int _nextStemNameIndex = 1;
        private readonly Dictionary<StemName, string> _stemNames;
        private int _nextCharDefIndex = 1;
        private readonly Dictionary<CharacterDefinition, string> _charDefs;
        private int _nextNaturalClassIndex = 1;
        private readonly Dictionary<NaturalClass, string> _naturalClasses;
        private int _nextFamilyIndex = 1;
        private readonly Dictionary<LexFamily, string> _families;
        private int _nextPhonologicalRuleIndex = 1;
        private readonly Dictionary<IPhonologicalRule, string> _phonologicalRules;
        private int _nextMorphologicalRuleIndex = 1;
        private int _nextMorphologicalSubruleIndex = 1;
        private int _nextEntryIndex = 1;
        private int _nextVariableFeatureIndex = 1;
        private int _nextAllomorphIndex = 1;
        private readonly Dictionary<Allomorph, string> _allomorphs;
        private readonly Dictionary<Morpheme, string> _morphemes;

        private XmlLanguageWriter(Language language)
        {
            _language = language;
            _tables = new Dictionary<CharacterDefinitionTable, string>();
            _stemNames = new Dictionary<StemName, string>();
            _mprFeatures = new Dictionary<MprFeature, string>();
            _allomorphs = new Dictionary<Allomorph, string>();
            _morphemes = new Dictionary<Morpheme, string>();
            _charDefs = new Dictionary<CharacterDefinition, string>();
            _naturalClasses = new Dictionary<NaturalClass, string>();
            _families = new Dictionary<LexFamily, string>();
            _phonologicalRules = new Dictionary<IPhonologicalRule, string>();
        }

        private void Save(TextWriter writer)
        {
            var doc = new XDocument(
                new XDocumentType("HermitCrabInput", null, "HermitCrabInput.dtd", null),
                new XElement("HermitCrabInput", WriteLanguage())
            );
            doc.Save(writer);
        }

        private XElement WriteLanguage()
        {
            var langElem = new XElement("Language", new XElement("Name", Normalize(_language.Name)));

            langElem.Add(
                new XElement(
                    "PartsOfSpeech",
                    _language.SyntacticFeatureSystem.PartOfSpeechFeature.PossibleSymbols.Select(WritePartOfSpeech)
                )
            );

            if (_language.PhonologicalFeatureSystem.Count > 0)
            {
                langElem.Add(
                    new XElement("PhonologicalFeatureSystem", _language.PhonologicalFeatureSystem.Select(WriteFeature))
                );
            }

            if (_language.SyntacticFeatureSystem.HeadFeature != null)
            {
                langElem.Add(
                    new XElement("HeadFeatures", _language.SyntacticFeatureSystem.HeadFeatures.Select(WriteFeature))
                );
            }

            if (_language.SyntacticFeatureSystem.FootFeature != null)
            {
                langElem.Add(
                    new XElement("FootFeatures", _language.SyntacticFeatureSystem.FootFeatures.Select(WriteFeature))
                );
            }

            if (_language.MprFeatures.Count > 0)
            {
                langElem.Add(
                    new XElement(
                        "MorphologicalPhonologicalRuleFeatures",
                        _language.MprFeatures.Select(WriteMprFeature),
                        _language.MprFeatureGroups.Select(WriteMprFeatureGroup)
                    )
                );
            }

            if (_language.StemNames.Count > 0)
                langElem.Add(new XElement("StemNames", _language.StemNames.Select(WriteStemName)));

            langElem.Add(_language.CharacterDefinitionTables.Select(WriteCharacterDefinitionTable));

            if (_language.NaturalClasses.Count > 0)
                langElem.Add(new XElement("NaturalClasses", _language.NaturalClasses.Select(WriteNaturalClass)));

            if (_language.Families.Count > 0)
                langElem.Add(new XElement("Families", _language.Families.Select(WriteFamily)));

            if (_language.PhonologicalRules.Count > 0)
            {
                langElem.Add(
                    new XElement(
                        "PhonologicalRuleDefinitions",
                        _language.PhonologicalRules.Select(WritePhonologicalRule)
                    )
                );
            }

            langElem.Add(new XElement("Strata", _language.Strata.Select(WriteStratum)));

            if (_language.MorphemeCoOccurrenceRules.Count > 0)
            {
                langElem.Add(
                    new XElement(
                        "MorphemeCoOccurrenceRules",
                        _language.MorphemeCoOccurrenceRules.Select(t => WriteMorphemeCoOccurrenceRule(t.Key, t.Rule))
                    )
                );
            }

            if (_language.AllomorphCoOccurrenceRules.Count > 0)
            {
                langElem.Add(
                    new XElement(
                        "AllomorphCoOccurrenceRules",
                        _language.AllomorphCoOccurrenceRules.Select(t => WriteAllomorphCoOccurrenceRule(t.Key, t.Rule))
                    )
                );
            }

            return langElem;
        }

        private XElement WritePartOfSpeech(FeatureSymbol pos)
        {
            return new XElement(
                "PartOfSpeech",
                new XAttribute("id", Normalize(pos.ID)),
                new XElement("Name", Normalize(pos.Description))
            );
        }

        private XElement WriteFeature(Feature feature)
        {
            if (feature is SymbolicFeature symbolicFeature)
            {
                var symFeatElem = new XElement("SymbolicFeature", new XAttribute("id", Normalize(symbolicFeature.ID)));
                if (symbolicFeature.DefaultValue != null)
                {
                    symFeatElem.Add(
                        new XAttribute(
                            "defaultSymbol",
                            Normalize(((SymbolicFeatureValue)symbolicFeature.DefaultValue).Values.First().ID)
                        )
                    );
                }

                symFeatElem.Add(new XElement("Name", Normalize(symbolicFeature.Description)));
                symFeatElem.Add(
                    new XElement(
                        "Symbols",
                        symbolicFeature.PossibleSymbols.Select(symbol => new XElement(
                            "Symbol",
                            new XAttribute("id", Normalize(symbol.ID)),
                            Normalize(symbol.Description)
                        ))
                    )
                );
                return symFeatElem;
            }

            return new XElement(
                "ComplexFeature",
                new XAttribute("id", Normalize(feature.ID)),
                new XElement("Name", Normalize(feature.Description))
            );
        }

        private XElement WriteMprFeature(MprFeature mprFeature)
        {
            string id = "mpr" + _nextMprFeatureIndex++;
            _mprFeatures[mprFeature] = id;
            return new XElement(
                "MorphologicalPhonologicalRuleFeature",
                new XAttribute("id", id),
                Normalize(mprFeature.Name)
            );
        }

        private XElement WriteMprFeatureGroup(MprFeatureGroup mprFeatureGroup)
        {
            string matchTypeStr = "";
            switch (mprFeatureGroup.MatchType)
            {
                case MprFeatureGroupMatchType.All:
                    matchTypeStr = "all";
                    break;
                case MprFeatureGroupMatchType.Any:
                    matchTypeStr = "any";
                    break;
            }

            string outputTypeStr = "";
            switch (mprFeatureGroup.Output)
            {
                case MprFeatureGroupOutput.Append:
                    outputTypeStr = "append";
                    break;
                case MprFeatureGroupOutput.Overwrite:
                    outputTypeStr = "overwrite";
                    break;
            }

            var mprGroupElem = new XElement("MorphologicalPhonologicalRuleFeatureGroup");
            if (mprFeatureGroup.MatchType == MprFeatureGroupMatchType.All)
                mprGroupElem.Add(new XAttribute("matchType", matchTypeStr));
            if (mprFeatureGroup.Output == MprFeatureGroupOutput.Append)
                mprGroupElem.Add(new XAttribute("outputType", outputTypeStr));
            mprGroupElem.Add(WriteIDs("features", mprFeatureGroup.MprFeatures, _mprFeatures));
            mprGroupElem.Add(new XElement("Name", Normalize(mprFeatureGroup.Name)));
            return mprGroupElem;
        }

        private XElement WriteStemName(StemName stemName)
        {
            string id = "stemName" + _nextStemNameIndex++;
            _stemNames[stemName] = id;
            return new XElement(
                "StemName",
                new XAttribute("id", id),
                new XAttribute(
                    "partsOfSpeech",
                    string.Join(" ", stemName.Regions.First().PartsOfSpeech().Select(pos => Normalize(pos.ID)))
                ),
                new XElement("Name", Normalize(stemName.Name)),
                new XElement("Regions", stemName.Regions.Select(WriteRegion))
            );
        }

        private XElement WriteRegion(FeatureStruct region)
        {
            var regionElem = new XElement("Region");
            FeatureStruct headFS = region.Head();
            if (headFS != null)
                regionElem.Add(new XElement("AssignedHeadFeatures", WriteFeatureStruct(headFS)));
            FeatureStruct footFS = region.Foot();
            if (footFS != null)
                regionElem.Add(new XElement("AssignedFootFeatures", WriteFeatureStruct(footFS)));
            return regionElem;
        }

        private XElement WriteCharacterDefinitionTable(CharacterDefinitionTable table)
        {
            string id = "table" + _nextTableIndex++;
            _tables[table] = id;
            var tableElem = new XElement(
                "CharacterDefinitionTable",
                new XAttribute("id", id),
                new XElement("Name", Normalize(table.Name)),
                new XElement(
                    "SegmentDefinitions",
                    table.Where(cd => cd.Type == HCFeatureSystem.Segment).Select(WriteSegmentDefinition)
                )
            );
            CharacterDefinition[] boundaries = table.Where(cd => cd.Type == HCFeatureSystem.Boundary).ToArray();
            if (boundaries.Length > 0)
                tableElem.Add(new XElement("BoundaryDefinitions", boundaries.Select(WriteBoundaryDefinition)));
            return tableElem;
        }

        private XElement WriteSegmentDefinition(CharacterDefinition segDef)
        {
            string id = "char" + _nextCharDefIndex++;
            _charDefs[segDef] = id;
            return new XElement(
                "SegmentDefinition",
                new XAttribute("id", id),
                new XElement(
                    "Representations",
                    segDef.Representations.Select(rep => new XElement("Representation", Normalize(rep)))
                ),
                WriteFeatureStruct(segDef.FeatureStruct)
            );
        }

        private XElement WriteBoundaryDefinition(CharacterDefinition bdryDef)
        {
            string id = "char" + _nextCharDefIndex++;
            _charDefs[bdryDef] = id;
            return new XElement(
                "BoundaryDefinition",
                new XAttribute("id", id),
                new XElement(
                    "Representations",
                    bdryDef.Representations.Select(rep => new XElement("Representation", Normalize(rep)))
                )
            );
        }

        private XElement WriteNaturalClass(NaturalClass nc)
        {
            string id = "nc" + _nextNaturalClassIndex++;
            _naturalClasses[nc] = id;

            if (nc is SegmentNaturalClass segmentNC)
            {
                return new XElement(
                    "SegmentNaturalClass",
                    new XAttribute("id", id),
                    new XElement("Name", Normalize(segmentNC.Name)),
                    segmentNC.Segments.Select(seg => WriteSegment(seg, null))
                );
            }

            return new XElement(
                "FeatureNaturalClass",
                new XAttribute("id", id),
                new XElement("Name", Normalize(nc.Name)),
                WriteFeatureStruct(nc.FeatureStruct)
            );
        }

        private XElement WriteFamily(LexFamily family)
        {
            string id = "family" + _nextFamilyIndex++;
            _families[family] = id;
            return new XElement("Family", new XAttribute("id", id), Normalize(family.Name));
        }

        private XElement WritePhonologicalRule(IPhonologicalRule prule)
        {
            if (prule is RewriteRule rewriteRule)
                return WritePhonologicalRule(rewriteRule);

            return WriteMetathesisRule((MetathesisRule)prule);
        }

        private XElement WritePhonologicalRule(RewriteRule rewriteRule)
        {
            string id = "prule" + _nextPhonologicalRuleIndex++;
            _phonologicalRules[rewriteRule] = id;
            var pruleElem = new XElement("PhonologicalRule", new XAttribute("id", id));
            string multipleAppOrderStr = "";
            switch (rewriteRule.ApplicationMode)
            {
                case RewriteApplicationMode.Iterative:
                    multipleAppOrderStr =
                        rewriteRule.Direction == Direction.LeftToRight
                            ? "leftToRightIterative"
                            : "rightToLeftIterative";
                    break;
                case RewriteApplicationMode.Simultaneous:
                    multipleAppOrderStr = "simultaneous";
                    break;
            }
            if (multipleAppOrderStr != "leftToRightIterative")
                pruleElem.Add(new XAttribute("multipleApplicationOrder", multipleAppOrderStr));
            pruleElem.Add(new XElement("Name", Normalize(rewriteRule.Name)));

            var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();
            var phonInputSeqElem = new XElement("PhoneticInput");
            if (!rewriteRule.Lhs.IsEmpty)
                phonInputSeqElem.Add(WritePhoneticSequence(rewriteRule.Lhs, variables));

            var subrulesElem = new XElement(
                "PhonologicalSubrules",
                rewriteRule.Subrules.Select(sr => WritePhonologicalSubrule(sr, variables))
            );
            if (variables.Count > 0)
                pruleElem.Add(WriteVariableFeatures(variables));
            pruleElem.Add(phonInputSeqElem);
            pruleElem.Add(subrulesElem);
            return pruleElem;
        }

        private XElement WritePhonologicalSubrule(
            RewriteSubrule subrule,
            Dictionary<string, Tuple<string, SymbolicFeature>> variables
        )
        {
            var subruleElem = new XElement("PhonologicalSubrule");
            WritePartsOfSpeechIfPresent(subruleElem, "requiredPartsOfSpeech", subrule.RequiredSyntacticFeatureStruct);
            if (subrule.RequiredMprFeatures.Count > 0)
                subruleElem.Add(WriteIDs("requiredMPRFeatures", subrule.RequiredMprFeatures, _mprFeatures));
            if (subrule.ExcludedMprFeatures.Count > 0)
                subruleElem.Add(WriteIDs("excludedMPRFeatures", subrule.ExcludedMprFeatures, _mprFeatures));
            var outputElem = new XElement("PhoneticOutput");
            if (!subrule.Rhs.IsEmpty)
                outputElem.Add(WritePhoneticSequence(subrule.Rhs, variables));
            subruleElem.Add(outputElem);

            if (!subrule.LeftEnvironment.IsEmpty || !subrule.RightEnvironment.IsEmpty)
            {
                var envElem = new XElement("Environment");
                if (!subrule.LeftEnvironment.IsEmpty)
                {
                    envElem.Add(
                        new XElement("LeftEnvironment", WritePhoneticTemplate(subrule.LeftEnvironment, variables))
                    );
                }

                if (!subrule.RightEnvironment.IsEmpty)
                {
                    envElem.Add(
                        new XElement("RightEnvironment", WritePhoneticTemplate(subrule.RightEnvironment, variables))
                    );
                }

                subruleElem.Add(envElem);
            }

            return subruleElem;
        }

        private XElement WriteMetathesisRule(MetathesisRule metathesisRule)
        {
            string id = "prule" + _nextPhonologicalRuleIndex++;
            _phonologicalRules[metathesisRule] = id;
            string prefix = id + "_";
            var metathesisRuleElem = new XElement(
                "MetathesisRule",
                new XAttribute("id", id),
                new XAttribute("leftSwitch", Normalize(prefix + metathesisRule.LeftSwitchName)),
                new XAttribute("rightSwitch", Normalize(prefix + metathesisRule.RightSwitchName))
            );
            if (metathesisRule.Direction == Direction.RightToLeft)
                metathesisRuleElem.Add(new XAttribute("multipleApplicationOrder", "rightToLeftIterative"));
            metathesisRuleElem.Add(new XElement("Name", Normalize(metathesisRule.Name)));
            metathesisRuleElem.Add(
                new XElement("StructuralDescription", WritePhoneticTemplate(metathesisRule.Pattern, null, null, prefix))
            );
            return metathesisRuleElem;
        }

        private XElement WriteVariableFeatures(Dictionary<string, Tuple<string, SymbolicFeature>> variables)
        {
            return new XElement(
                "VariableFeatures",
                variables.Select(kvp => new XElement(
                    "VariableFeature",
                    new XAttribute("id", kvp.Value.Item1),
                    new XAttribute("name", kvp.Key),
                    new XAttribute("phonologicalFeature", Normalize(kvp.Value.Item2.ID))
                ))
            );
        }

        private IEnumerable<XElement> WriteFeatureStruct(FeatureStruct fs)
        {
            foreach (
                Feature feature in fs.Features.Where(f => !f.IsOneOf(HCFeatureSystem.Type, HCFeatureSystem.StrRep))
            )
            {
                var fvElem = new XElement("FeatureValue", new XAttribute("feature", Normalize(feature.ID)));
                if (feature is SymbolicFeature symbolicFeature)
                {
                    SymbolicFeatureValue value = fs.GetValue(symbolicFeature);
                    fvElem.Add(
                        new XAttribute("symbolValues", string.Join(" ", value.Values.Select(v => Normalize(v.ID))))
                    );
                }
                else
                {
                    var complexFeature = (ComplexFeature)feature;
                    FeatureStruct childFS = fs.GetValue(complexFeature);
                    fvElem.Add(WriteFeatureStruct(childFS));
                }
                yield return fvElem;
            }
        }

        private XElement WriteStratum(Stratum stratum)
        {
            var mrules = new Dictionary<IMorphologicalRule, string>();
            foreach (
                IMorphologicalRule mrule in stratum.MorphologicalRules.Concat(
                    stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules).Distinct()
                )
            )
            {
                mrules[mrule] = "mrule" + _nextMorphologicalRuleIndex++;
            }

            var stratumElem = new XElement("Stratum");
            stratumElem.Add(new XAttribute("characterDefinitionTable", _tables[stratum.CharacterDefinitionTable]));
            if (stratum.MorphologicalRuleOrder == MorphologicalRuleOrder.Unordered)
                stratumElem.Add(new XAttribute("morphologicalRuleOrder", "unordered"));
            if (stratum.PhonologicalRules.Count > 0)
                stratumElem.Add(WriteIDs("phonologicalRules", stratum.PhonologicalRules, _phonologicalRules));
            if (stratum.MorphologicalRules.Count > 0)
                stratumElem.Add(WriteIDs("morphologicalRules", stratum.MorphologicalRules, mrules));
            stratumElem.Add(new XElement("Name", Normalize(stratum.Name)));

            if (mrules.Count > 0)
            {
                stratumElem.Add(
                    new XElement(
                        "MorphologicalRuleDefinitions",
                        mrules.Keys.Select(mrule => WriteMorphologicalRule(mrule, mrules))
                    )
                );
            }

            if (stratum.AffixTemplates.Count > 0)
            {
                stratumElem.Add(
                    new XElement("AffixTemplates", stratum.AffixTemplates.Select(t => WriteAffixTemplate(t, mrules)))
                );
            }

            if (stratum.Entries.Count > 0)
                stratumElem.Add(new XElement("LexicalEntries", stratum.Entries.Select(WriteLexicalEntry)));

            return stratumElem;
        }

        private XElement WriteMorphologicalRule(IMorphologicalRule mrule, Dictionary<IMorphologicalRule, string> mrules)
        {
            if (mrule is AffixProcessRule affixProcessRule)
                return WriteMorphologicalRule(affixProcessRule, mrules);

            if (mrule is RealizationalAffixProcessRule realRule)
                return WriteRealizationalRule(realRule, mrules);

            return WriteCompoundingRule((CompoundingRule)mrule, mrules);
        }

        private XElement WriteMorphologicalRule(
            AffixProcessRule affixProcessRule,
            Dictionary<IMorphologicalRule, string> mrules
        )
        {
            string id = mrules[affixProcessRule];
            _morphemes[affixProcessRule] = id;
            var ruleElem = new XElement("MorphologicalRule", new XAttribute("id", id));

            if (!affixProcessRule.Blockable)
                ruleElem.Add(new XAttribute("blockable", "false"));

            if (affixProcessRule.MaxApplicationCount != 1)
                ruleElem.Add(new XAttribute("multipleApplication", affixProcessRule.MaxApplicationCount));

            WritePartsOfSpeechIfPresent(
                ruleElem,
                "requiredPartsOfSpeech",
                affixProcessRule.RequiredSyntacticFeatureStruct
            );

            if (affixProcessRule.RequiredStemName != null)
                ruleElem.Add(new XAttribute("requiredStemName", _stemNames[affixProcessRule.RequiredStemName]));

            WritePartsOfSpeechIfPresent(ruleElem, "outputPartOfSpeech", affixProcessRule.OutSyntacticFeatureStruct);

            if (affixProcessRule.ObligatorySyntacticFeatures.Count > 0)
            {
                ruleElem.Add(
                    new XAttribute(
                        "outputObligatoryFeatures",
                        string.Join(" ", affixProcessRule.ObligatorySyntacticFeatures.Select(f => Normalize(f.ID)))
                    )
                );
            }

            if (affixProcessRule.IsPartial)
                ruleElem.Add(new XAttribute("partial", "true"));

            ruleElem.Add(new XElement("Name", Normalize(affixProcessRule.Name)));

            ruleElem.Add(
                new XElement(
                    "MorphologicalSubrules",
                    affixProcessRule.Allomorphs.Select(sr =>
                        WriteMorphologicalSubrule(sr, affixProcessRule.Stratum.CharacterDefinitionTable)
                    )
                )
            );

            FeatureStruct outHeadFS = affixProcessRule.OutSyntacticFeatureStruct.Head();
            if (outHeadFS != null)
                ruleElem.Add(new XElement("OutputHeadFeatures", WriteFeatureStruct(outHeadFS)));
            FeatureStruct outFootFS = affixProcessRule.OutSyntacticFeatureStruct.Foot();
            if (outFootFS != null)
                ruleElem.Add(new XElement("OutputFootFeatures", WriteFeatureStruct(outFootFS)));

            FeatureStruct requiredHeadFS = affixProcessRule.RequiredSyntacticFeatureStruct.Head();
            if (requiredHeadFS != null)
                ruleElem.Add(new XElement("RequiredHeadFeatures", WriteFeatureStruct(requiredHeadFS)));
            FeatureStruct requiredFootFS = affixProcessRule.RequiredSyntacticFeatureStruct.Foot();
            if (requiredFootFS != null)
                ruleElem.Add(new XElement("RequiredFootFeatures", WriteFeatureStruct(requiredFootFS)));

            WriteMorphemeElements(ruleElem, affixProcessRule);
            return ruleElem;
        }

        private XElement WriteMorphologicalSubrule(
            AffixProcessAllomorph allomorph,
            CharacterDefinitionTable defaultTable
        )
        {
            string id = "msubrule" + _nextMorphologicalSubruleIndex++;
            _allomorphs[allomorph] = id;
            var subruleElem = new XElement("MorphologicalSubrule", new XAttribute("id", id));

            var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();
            var inputElem = new XElement("MorphologicalInput");
            if (allomorph.RequiredMprFeatures.Count > 0)
                inputElem.Add(WriteIDs("requiredMPRFeatures", allomorph.RequiredMprFeatures, _mprFeatures));
            if (allomorph.ExcludedMprFeatures.Count > 0)
                inputElem.Add(WriteIDs("excludedMPRFeatures", allomorph.ExcludedMprFeatures, _mprFeatures));
            string prefix = id + "_";
            inputElem.Add(allomorph.Lhs.Select(p => WritePhoneticSequence(p, variables, defaultTable, prefix)));

            var outputElem = new XElement("MorphologicalOutput");
            if (allomorph.OutMprFeatures.Count > 0)
                outputElem.Add(WriteIDs("MPRFeatures", allomorph.OutMprFeatures, _mprFeatures));
            string redupMorphTypeStr = "";
            switch (allomorph.ReduplicationHint)
            {
                case ReduplicationHint.Prefix:
                    redupMorphTypeStr = "prefix";
                    break;
                case ReduplicationHint.Suffix:
                    redupMorphTypeStr = "suffix";
                    break;
                case ReduplicationHint.Implicit:
                    redupMorphTypeStr = "implicit";
                    break;
            }
            if (redupMorphTypeStr != "implicit")
                outputElem.Add(new XAttribute("redupMorphType", redupMorphTypeStr));
            outputElem.Add(
                allomorph.Rhs.Select(a => WriteMorphologicalOutputAction(a, variables, prefix, defaultTable))
            );

            if (variables.Count > 0)
                subruleElem.Add(WriteVariableFeatures(variables));
            subruleElem.Add(inputElem);
            subruleElem.Add(outputElem);

            FeatureStruct headFS = allomorph.RequiredSyntacticFeatureStruct.Head();
            if (headFS != null)
                subruleElem.Add(new XElement("RequiredHeadFeatures", WriteFeatureStruct(headFS)));

            FeatureStruct footFS = allomorph.RequiredSyntacticFeatureStruct.Foot();
            if (footFS != null)
                subruleElem.Add(new XElement("RequiredFootFeatures", WriteFeatureStruct(footFS)));

            WriteAllomorphElements(subruleElem, allomorph, defaultTable);
            return subruleElem;
        }

        private XElement WriteRealizationalRule(
            RealizationalAffixProcessRule realRule,
            Dictionary<IMorphologicalRule, string> mrules
        )
        {
            string id = mrules[realRule];
            _morphemes[realRule] = id;
            var ruleElem = new XElement("RealizationalRule", new XAttribute("id", id));

            if (!realRule.Blockable)
                ruleElem.Add(new XAttribute("blockable", "false"));

            ruleElem.Add(new XElement("Name", Normalize(realRule.Name)));

            ruleElem.Add(
                new XElement(
                    "MorphologicalSubrules",
                    realRule.Allomorphs.Select(sr =>
                        WriteMorphologicalSubrule(sr, realRule.Stratum.CharacterDefinitionTable)
                    )
                )
            );

            FeatureStruct realHeadFS = realRule.RealizationalFeatureStruct.Head();
            if (realHeadFS != null)
                ruleElem.Add(new XElement("RealizationalFeatures", WriteFeatureStruct(realHeadFS)));

            FeatureStruct requiredHeadFS = realRule.RequiredSyntacticFeatureStruct.Head();
            if (requiredHeadFS != null)
                ruleElem.Add(new XElement("RequiredHeadFeatures", WriteFeatureStruct(requiredHeadFS)));
            FeatureStruct requiredFootFS = realRule.RequiredSyntacticFeatureStruct.Foot();
            if (requiredFootFS != null)
                ruleElem.Add(new XElement("RequiredFootFeatures", WriteFeatureStruct(requiredFootFS)));

            WriteMorphemeElements(ruleElem, realRule);
            return ruleElem;
        }

        private XElement WriteCompoundingRule(CompoundingRule crule, Dictionary<IMorphologicalRule, string> mrules)
        {
            string id = mrules[crule];
            var ruleElem = new XElement("CompoundingRule", new XAttribute("id", id));

            if (!crule.Blockable)
                ruleElem.Add(new XAttribute("blockable", "false"));

            if (crule.MaxApplicationCount != 1)
                ruleElem.Add(new XAttribute("multipleApplication", crule.MaxApplicationCount));

            WritePartsOfSpeechIfPresent(ruleElem, "headPartsOfSpeech", crule.HeadRequiredSyntacticFeatureStruct);
            WritePartsOfSpeechIfPresent(ruleElem, "nonHeadPartsOfSpeech", crule.NonHeadRequiredSyntacticFeatureStruct);

            WritePartsOfSpeechIfPresent(ruleElem, "outputPartOfSpeech", crule.OutSyntacticFeatureStruct);

            if (crule.ObligatorySyntacticFeatures.Count > 0)
            {
                ruleElem.Add(
                    new XAttribute(
                        "outputObligatoryFeatures",
                        string.Join(" ", crule.ObligatorySyntacticFeatures.Select(f => Normalize(f.ID)))
                    )
                );
            }

            ruleElem.Add(new XElement("Name", Normalize(crule.Name)));

            ruleElem.Add(
                new XElement(
                    "CompoundingSubrules",
                    crule.Subrules.Select(
                        (sr, i) =>
                            WriteCompoundingSubrule(
                                sr,
                                string.Format("{0}_{1}_", id, i + 1),
                                crule.Stratum.CharacterDefinitionTable
                            )
                    )
                )
            );

            FeatureStruct outHeadFS = crule.OutSyntacticFeatureStruct.Head();
            if (outHeadFS != null)
                ruleElem.Add(new XElement("OutputHeadFeatures", WriteFeatureStruct(outHeadFS)));
            FeatureStruct outFootFS = crule.OutSyntacticFeatureStruct.Foot();
            if (outFootFS != null)
                ruleElem.Add(new XElement("OutputFootFeatures", WriteFeatureStruct(outFootFS)));

            FeatureStruct headRequiredHeadFS = crule.HeadRequiredSyntacticFeatureStruct.Head();
            if (headRequiredHeadFS != null)
                ruleElem.Add(new XElement("HeadRequiredHeadFeatures", WriteFeatureStruct(headRequiredHeadFS)));
            FeatureStruct nonHeadRequiredHeadFS = crule.NonHeadRequiredSyntacticFeatureStruct.Head();
            if (nonHeadRequiredHeadFS != null)
                ruleElem.Add(new XElement("NonHeadRequiredHeadFeatures", WriteFeatureStruct(nonHeadRequiredHeadFS)));

            FeatureStruct headRequiredFootFS = crule.HeadRequiredSyntacticFeatureStruct.Foot();
            if (headRequiredFootFS != null)
                ruleElem.Add(new XElement("HeadRequiredFootFeatures", WriteFeatureStruct(headRequiredFootFS)));
            FeatureStruct nonHeadRequiredFootFS = crule.NonHeadRequiredSyntacticFeatureStruct.Foot();
            if (nonHeadRequiredFootFS != null)
                ruleElem.Add(new XElement("NonHeadRequiredFootFeatures", WriteFeatureStruct(nonHeadRequiredFootFS)));

            return ruleElem;
        }

        private XElement WriteCompoundingSubrule(
            CompoundingSubrule subrule,
            string partPrefix,
            CharacterDefinitionTable defaultTable
        )
        {
            var subruleElem = new XElement("CompoundingSubrule");

            var variables = new Dictionary<string, Tuple<string, SymbolicFeature>>();
            var headInputElem = new XElement("HeadMorphologicalInput");
            if (subrule.RequiredMprFeatures.Count > 0)
                headInputElem.Add(WriteIDs("requiredMPRFeatures", subrule.RequiredMprFeatures, _mprFeatures));
            if (subrule.ExcludedMprFeatures.Count > 0)
                headInputElem.Add(WriteIDs("excludedMPRFeatures", subrule.ExcludedMprFeatures, _mprFeatures));

            headInputElem.Add(
                subrule.HeadLhs.Select(p => WritePhoneticSequence(p, variables, defaultTable, partPrefix))
            );

            var nonHeadInputElem = new XElement(
                "NonHeadMorphologicalInput",
                subrule.NonHeadLhs.Select(p => WritePhoneticSequence(p, variables, defaultTable, partPrefix))
            );

            var outputElem = new XElement("MorphologicalOutput");
            if (subrule.OutMprFeatures.Count > 0)
                outputElem.Add(WriteIDs("MPRFeatures", subrule.OutMprFeatures, _mprFeatures));
            outputElem.Add(
                subrule.Rhs.Select(a => WriteMorphologicalOutputAction(a, variables, partPrefix, defaultTable))
            );

            if (variables.Count > 0)
                subruleElem.Add(WriteVariableFeatures(variables));
            subruleElem.Add(headInputElem);
            subruleElem.Add(nonHeadInputElem);
            subruleElem.Add(outputElem);

            return subruleElem;
        }

        private XElement WriteMorphologicalOutputAction(
            MorphologicalOutputAction action,
            Dictionary<string, Tuple<string, SymbolicFeature>> variables,
            string prefix,
            CharacterDefinitionTable defaultTable
        )
        {
            if (action is CopyFromInput copy)
            {
                return new XElement("CopyFromInput", new XAttribute("index", Normalize(prefix + copy.PartName)));
            }

            if (action is InsertSimpleContext insertShapeNode)
            {
                return new XElement(
                    "InsertSimpleContext",
                    WriteSimpleContext(insertShapeNode.SimpleContext, variables)
                );
            }

            if (action is ModifyFromInput modify)
            {
                return new XElement(
                    "ModifyFromInput",
                    new XAttribute("index", Normalize(prefix + modify.PartName)),
                    WriteSimpleContext(modify.SimpleContext, variables)
                );
            }

            var insertSegments = (InsertSegments)action;
            var insertSegmentsElem = new XElement("InsertSegments");
            if (insertSegments.Segments.CharacterDefinitionTable != defaultTable)
            {
                insertSegmentsElem.Add(
                    new XAttribute(
                        "characterDefinitionTable",
                        _tables[insertSegments.Segments.CharacterDefinitionTable]
                    )
                );
            }

            insertSegmentsElem.Add(new XElement("PhoneticShape", Normalize(insertSegments.Segments.Representation)));
            return insertSegmentsElem;
        }

        private XElement WriteAffixTemplate(AffixTemplate template, Dictionary<IMorphologicalRule, string> mrules)
        {
            var templateElem = new XElement("AffixTemplate");

            if (!template.IsFinal)
                templateElem.Add(new XAttribute("final", "false"));

            WritePartsOfSpeechIfPresent(templateElem, "requiredPartsOfSpeech", template.RequiredSyntacticFeatureStruct);

            templateElem.Add(new XElement("Name", Normalize(template.Name)));

            foreach (AffixTemplateSlot slot in template.Slots)
            {
                var slotElem = new XElement("Slot");
                if (slot.Optional)
                    slotElem.Add(new XAttribute("optional", "true"));
                slotElem.Add(WriteIDs("morphologicalRules", slot.Rules, mrules));
                slotElem.Add(new XElement("Name", Normalize(slot.Name)));
                templateElem.Add(slotElem);
            }
            return templateElem;
        }

        private XElement WriteLexicalEntry(LexEntry entry)
        {
            string id = "entry" + _nextEntryIndex++;
            _morphemes[entry] = id;
            var entryElem = new XElement("LexicalEntry", new XAttribute("id", id));

            if (entry.Family != null)
                entryElem.Add(new XAttribute("family", _families[entry.Family]));

            WritePartsOfSpeechIfPresent(entryElem, "partOfSpeech", entry.SyntacticFeatureStruct);

            if (entry.MprFeatures.Count > 0)
                entryElem.Add(WriteIDs("ruleFeatures", entry.MprFeatures, _mprFeatures));

            if (entry.IsPartial)
                entryElem.Add(new XAttribute("partial", "true"));

            entryElem.Add(
                new XElement(
                    "Allomorphs",
                    entry.Allomorphs.Select(a => WriteAllomorph(a, entry.Stratum.CharacterDefinitionTable))
                )
            );

            FeatureStruct headFS = entry.SyntacticFeatureStruct.Head();
            if (headFS != null)
                entryElem.Add(new XElement("AssignedHeadFeatures", WriteFeatureStruct(headFS)));

            FeatureStruct footFS = entry.SyntacticFeatureStruct.Foot();
            if (footFS != null)
                entryElem.Add(new XElement("AssignedFootFeatures", WriteFeatureStruct(footFS)));

            WriteMorphemeElements(entryElem, entry);

            return entryElem;
        }

        private XElement WriteAllomorph(RootAllomorph allomorph, CharacterDefinitionTable defaultTable)
        {
            string id = "allo" + _nextAllomorphIndex++;
            _allomorphs[allomorph] = id;
            var alloElem = new XElement("Allomorph", new XAttribute("id", id));
            if (allomorph.StemName != null)
                alloElem.Add(new XAttribute("stemName", _stemNames[allomorph.StemName]));
            if (allomorph.IsBound)
                alloElem.Add(new XAttribute("isBound", allomorph.IsBound));
            alloElem.Add(new XElement("PhoneticShape", Normalize(allomorph.Segments.Representation)));
            WriteAllomorphElements(alloElem, allomorph, defaultTable);
            return alloElem;
        }

        private void WriteMorphemeElements(XElement morphemeElem, Morpheme morpheme)
        {
            if (!string.IsNullOrEmpty(morpheme.Id))
                morphemeElem.Add(new XElement("MorphemeId", morpheme.Id));
            if (!string.IsNullOrEmpty(morpheme.Gloss))
                morphemeElem.Add(new XElement("Gloss", Normalize(morpheme.Gloss)));
            if (morpheme.Properties.Count > 0)
                morphemeElem.Add(WriteProperties(morpheme.Properties));
        }

        private XElement WriteMorphemeCoOccurrenceRule(Morpheme key, MorphemeCoOccurrenceRule coOccurRule)
        {
            var coOccurElem = new XElement("MorphemeCoOccurrenceRule");
            if (coOccurRule.Type == ConstraintType.Require)
                coOccurElem.Add(new XAttribute("type", GetConstraintTypeStr(coOccurRule.Type)));
            coOccurElem.Add(new XAttribute("primaryMorpheme", _morphemes[key]));
            coOccurElem.Add(
                new XAttribute("otherMorphemes", string.Join(" ", coOccurRule.Others.Select(m => _morphemes[m])))
            );
            if (coOccurRule.Adjacency != MorphCoOccurrenceAdjacency.Anywhere)
                coOccurElem.Add(new XAttribute("adjacency", GetMorphCoOccurrenceAdjacencyStr(coOccurRule.Adjacency)));
            return coOccurElem;
        }

        private void WriteAllomorphElements(
            XElement alloElem,
            Allomorph allomorph,
            CharacterDefinitionTable defaultTable
        )
        {
            AllomorphEnvironment[] requiredEnvs = allomorph
                .Environments.Where(e => e.Type == ConstraintType.Require)
                .ToArray();
            if (requiredEnvs.Length > 0)
            {
                alloElem.Add(
                    new XElement("RequiredEnvironments", requiredEnvs.Select(e => WriteEnvironment(e, defaultTable)))
                );
            }

            AllomorphEnvironment[] excludedEnvs = allomorph
                .Environments.Where(e => e.Type == ConstraintType.Exclude)
                .ToArray();
            if (excludedEnvs.Length > 0)
            {
                alloElem.Add(
                    new XElement("ExcludedEnvironments", excludedEnvs.Select(e => WriteEnvironment(e, defaultTable)))
                );
            }

            if (allomorph.Properties.Count > 0)
                alloElem.Add(WriteProperties(allomorph.Properties));
        }

        private XElement WriteEnvironment(AllomorphEnvironment env, CharacterDefinitionTable defaultTable)
        {
            var envElem = new XElement("Environment");
            if (env.LeftEnvironment != null)
            {
                envElem.Add(
                    new XElement("LeftEnvironment", WritePhoneticTemplate(env.LeftEnvironment, null, defaultTable))
                );
            }

            if (env.RightEnvironment != null)
            {
                envElem.Add(
                    new XElement("RightEnvironment", WritePhoneticTemplate(env.RightEnvironment, null, defaultTable))
                );
            }

            return envElem;
        }

        private XElement WriteAllomorphCoOccurrenceRule(Allomorph key, AllomorphCoOccurrenceRule coOccurRule)
        {
            var coOccurElem = new XElement("AllomorphCoOccurrenceRule");
            if (coOccurRule.Type == ConstraintType.Require)
                coOccurElem.Add(new XAttribute("type", GetConstraintTypeStr(coOccurRule.Type)));
            coOccurElem.Add(new XAttribute("primaryAllomorph", _allomorphs[key]));
            coOccurElem.Add(
                new XAttribute("otherAllomorphs", string.Join(" ", coOccurRule.Others.Select(a => _allomorphs[a])))
            );
            if (coOccurRule.Adjacency != MorphCoOccurrenceAdjacency.Anywhere)
                coOccurElem.Add(new XAttribute("adjacency", GetMorphCoOccurrenceAdjacencyStr(coOccurRule.Adjacency)));
            return coOccurElem;
        }

        private XElement WriteProperties(IDictionary<string, object> properties)
        {
            return new XElement(
                "Properties",
                properties.Select(kvp => new XElement(
                    "Property",
                    new XAttribute("name", Normalize(kvp.Key)),
                    Normalize(kvp.Value.ToString())
                ))
            );
        }

        private XElement WritePhoneticTemplate(
            Pattern<Word, ShapeNode> pattern,
            Dictionary<string, Tuple<string, SymbolicFeature>> variables,
            CharacterDefinitionTable defaultTable = null,
            string prefix = null
        )
        {
            var phonTempElem = new XElement("PhoneticTemplate");
            if (IsAnchor(pattern.Children.First, HCFeatureSystem.LeftSide))
                phonTempElem.Add(new XAttribute("initialBoundaryCondition", "true"));
            phonTempElem.Add(WritePhoneticSequence(pattern, variables, defaultTable, prefix));
            if (IsAnchor(pattern.Children.Last, HCFeatureSystem.RightSide))
                phonTempElem.Add(new XAttribute("finalBoundaryCondition", "true"));
            return phonTempElem;
        }

        private bool IsAnchor(PatternNode<Word, ShapeNode> node, FeatureSymbol type)
        {
            if (node is Constraint<Word, ShapeNode> constraint)
            {
                return constraint.Type() == HCFeatureSystem.Anchor
                    && (FeatureSymbol)constraint.FeatureStruct.GetValue(HCFeatureSystem.AnchorType) == type;
            }

            return false;
        }

        private XElement WritePhoneticSequence(
            Pattern<Word, ShapeNode> pattern,
            Dictionary<string, Tuple<string, SymbolicFeature>> variables,
            CharacterDefinitionTable defaultTable = null,
            string prefix = null
        )
        {
            var seqElem = new XElement("PhoneticSequence");
            if (!string.IsNullOrEmpty(pattern.Name))
                seqElem.Add(new XAttribute("id", Normalize((prefix ?? "") + pattern.Name)));
            foreach (PatternNode<Word, ShapeNode> node in pattern.Children)
                seqElem.Add(WritePatternNodes(node, variables, defaultTable, prefix ?? "", null));
            return seqElem;
        }

        private IEnumerable<XElement> WritePatternNodes(
            PatternNode<Word, ShapeNode> node,
            Dictionary<string, Tuple<string, SymbolicFeature>> variables,
            CharacterDefinitionTable defaultTable,
            string prefix,
            string id
        )
        {
            if (node is Constraint<Word, ShapeNode> constraint)
            {
                if (constraint.Tag == null)
                    yield break;

                if (constraint.Tag is CharacterDefinition charDef)
                {
                    yield return charDef.Type == HCFeatureSystem.Segment
                        ? WriteSegment(charDef, id)
                        : WriteBoundaryMarker(charDef, id);
                }
                else
                {
                    yield return WriteSimpleContext((SimpleContext)constraint.Tag, variables, id);
                }

                yield break;
            }

            if (node is Quantifier<Word, ShapeNode> quantifier)
            {
                yield return WriteOptionalSegmentSequence(quantifier, variables, defaultTable, id);
                yield break;
            }

            if (node is Group<Word, ShapeNode> group)
            {
                if (!string.IsNullOrEmpty(group.Name))
                {
                    // Metathesis Id group
                    foreach (
                        XElement elem in WritePatternNodes(
                            group.Children.First,
                            variables,
                            defaultTable,
                            prefix,
                            prefix + group.Name
                        )
                    )
                    {
                        yield return elem;
                    }
                }
                else if (group.Tag != null)
                {
                    // Segments group
                    yield return WriteSegments((Segments)group.Tag, id, defaultTable);
                }
                else
                {
                    // Normal group
                    foreach (PatternNode<Word, ShapeNode> childNode in group.Children)
                    {
                        foreach (XElement elem in WritePatternNodes(childNode, variables, defaultTable, prefix, id))
                            yield return elem;
                    }
                }
            }
        }

        private XElement WriteBoundaryMarker(CharacterDefinition charDef, string id)
        {
            var brdyElem = new XElement("BoundaryMarker");
            if (!string.IsNullOrEmpty(id))
                brdyElem.Add(new XAttribute("id", Normalize(id)));
            brdyElem.Add(new XAttribute("boundary", _charDefs[charDef]));
            return brdyElem;
        }

        private XElement WriteSegment(CharacterDefinition charDef, string id)
        {
            var segElem = new XElement("Segment");
            if (!string.IsNullOrEmpty(id))
                segElem.Add(new XAttribute("id", Normalize(id)));
            segElem.Add(new XAttribute("segment", _charDefs[charDef]));
            return segElem;
        }

        private XElement WriteSimpleContext(
            SimpleContext simpleCtxt,
            Dictionary<string, Tuple<string, SymbolicFeature>> variables,
            string id = null
        )
        {
            var ctxtElem = new XElement("SimpleContext");
            if (!string.IsNullOrEmpty(id))
                ctxtElem.Add(new XAttribute("id", Normalize(id)));
            ctxtElem.Add(new XAttribute("naturalClass", _naturalClasses[simpleCtxt.NaturalClass]));
            if (simpleCtxt.Variables.Count > 0)
            {
                var alphaVarsElem = new XElement("AlphaVariables");
                foreach (SymbolicFeatureValue ctxtVar in simpleCtxt.Variables)
                {
                    SymbolicFeature feature = ctxtVar.Feature;
                    Tuple<string, SymbolicFeature> var = variables.GetOrCreate(
                        ctxtVar.VariableName,
                        () => Tuple.Create("var" + _nextVariableFeatureIndex++, feature)
                    );
                    var alphVarElem = new XElement("AlphaVariable", new XAttribute("variableFeature", var.Item1));
                    if (!ctxtVar.Agree)
                        alphVarElem.Add(new XAttribute("polarity", "minus"));
                    alphaVarsElem.Add(alphVarElem);
                }
                ctxtElem.Add(alphaVarsElem);
            }
            return ctxtElem;
        }

        private XElement WriteOptionalSegmentSequence(
            Quantifier<Word, ShapeNode> quantifier,
            Dictionary<string, Tuple<string, SymbolicFeature>> variables,
            CharacterDefinitionTable defaultTable,
            string id
        )
        {
            var optSegSeqElem = new XElement("OptionalSegmentSequence");
            if (!string.IsNullOrEmpty(id))
                optSegSeqElem.Add(new XAttribute("id", Normalize(id)));
            optSegSeqElem.Add(new XAttribute("min", quantifier.MinOccur));
            optSegSeqElem.Add(new XAttribute("max", quantifier.MaxOccur));
            optSegSeqElem.Add(WritePatternNodes(quantifier.Children.First, variables, defaultTable, "", null));
            return optSegSeqElem;
        }

        private XElement WriteSegments(Segments segments, string id, CharacterDefinitionTable defaultTable)
        {
            var segsElem = new XElement("Segments");
            if (!string.IsNullOrEmpty(id))
                segsElem.Add(new XAttribute("id", Normalize(id)));
            if (segments.CharacterDefinitionTable != defaultTable)
                segsElem.Add(new XAttribute("characterDefinitionTable", _tables[segments.CharacterDefinitionTable]));
            segsElem.Add(new XElement("PhoneticShape", Normalize(segments.Representation)));
            return segsElem;
        }
    }
}
