<#
.SYNOPSIS
  Generates conformance/fieldworks-producibility.tsv: a ledger of whether FieldWorks' HCLoader
  (Src/LexText/ParserCore/HCLoader.cs in the FieldWorks repo) can ever produce each HC construct
  that the conformance suite's other ledgers measure.

.DESCRIPTION
  The SUBJECT LIST is extracted mechanically from two files already in this repo, so it can never
  be hand-typed out of sync with its source:
    - every SIL.Machine.Morphology.HermitCrab.FailureReason enum member (except None), read from
      src/SIL.Machine.Morphology.HermitCrab/ITraceManager.cs
    - every (element, attribute) row in conformance/interface-inventory.tsv

  The VERDICT for each subject (producible / hcloader_sites / notes) is NOT something a script can
  derive -- it required reading the whole of HCLoader.cs (FieldWorks repo, ~2837 lines) and cross-
  referencing this repo's own engine source (property definitions, XmlLanguageLoader.cs as the
  ground truth for which runtime property a DTD attribute maps to, and InteractionChainLedger.cs's
  own "dead" annotations). That research is embedded below as $Verdicts. Re-deriving it requires a
  human to re-read HCLoader.cs; this script only guarantees the OUTPUT is well-formed and complete
  against the two mechanically-extracted subject lists -- it cannot detect the FieldWorks repo
  changing underneath a verdict recorded here. See conformance/docs/how-it-is-computed.md.

  Every subject found in the two source files MUST have a verdict below, or this script throws --
  silently defaulting an unclassified subject would be exactly the wrong kind of "I could not look
  reads as everything is fine."
#>

#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$TraceManagerPath = Join-Path $RepoRoot "src\SIL.Machine.Morphology.HermitCrab\ITraceManager.cs"
$InterfaceInventoryPath = Join-Path $RepoRoot "conformance\interface-inventory.tsv"
$OutputPath = Join-Path $RepoRoot "conformance\fieldworks-producibility.tsv"

if (-not (Test-Path $TraceManagerPath)) { throw "Cannot find $TraceManagerPath" }
if (-not (Test-Path $InterfaceInventoryPath)) { throw "Cannot find $InterfaceInventoryPath" }

# ---------------------------------------------------------------------------
# 1. Mechanically extract the FailureReason subjects (23, excluding None).
# ---------------------------------------------------------------------------
$traceManagerText = Get-Content $TraceManagerPath -Raw
$enumMatch = [regex]::Match($traceManagerText, "public enum FailureReason\s*\{(?<body>[^}]*)\}")
if (-not $enumMatch.Success) { throw "Could not locate 'public enum FailureReason { ... }' in $TraceManagerPath" }
$failureReasons = $enumMatch.Groups["body"].Value -split "," |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -and $_ -ne "None" }

if ($failureReasons.Count -ne 23) {
    throw "Expected 23 FailureReason members besides None, found $($failureReasons.Count): $($failureReasons -join ', ')"
}

# ---------------------------------------------------------------------------
# 2. Mechanically extract the interface-inventory subjects (60 rows).
# ---------------------------------------------------------------------------
$inventoryLines = Get-Content $InterfaceInventoryPath | Where-Object { $_ -and -not $_.StartsWith("#") }
if ($inventoryLines.Count -lt 2) { throw "$InterfaceInventoryPath has no data rows" }
$header = $inventoryLines[0] -split "`t"
$elementCol = [array]::IndexOf($header, "element")
$attributeCol = [array]::IndexOf($header, "attribute")
if ($elementCol -lt 0 -or $attributeCol -lt 0) { throw "interface-inventory.tsv header missing element/attribute columns" }

$interfaceAttrs = @()
foreach ($line in $inventoryLines[1..($inventoryLines.Count - 1)]) {
    $cols = $line -split "`t"
    $interfaceAttrs += [PSCustomObject]@{
        Element   = $cols[$elementCol]
        Attribute = $cols[$attributeCol]
    }
}

if ($interfaceAttrs.Count -ne 60) {
    throw "Expected 60 interface-inventory rows, found $($interfaceAttrs.Count)"
}

# ---------------------------------------------------------------------------
# 3. Researched verdicts, keyed by subject_kind + subject. See the header
#    comment: this table is the product of reading the whole of HCLoader.cs
#    (FieldWorks repo) plus this repo's own engine source, not a derivation.
# ---------------------------------------------------------------------------
$Verdicts = @{}

function Add-Verdict([string]$Kind, [string]$Subject, [string]$Producible, [string[]]$Sites, [string]$Notes) {
    $key = "$Kind|$Subject"
    if ($Verdicts.ContainsKey($key)) { throw "Duplicate verdict for $key" }
    $Verdicts[$key] = [PSCustomObject]@{
        Producible = $Producible
        Sites      = ($Sites -join "; ")
        Notes      = $Notes
    }
}

# --- FailureReason subjects (subject_kind = failure-reason) -----------------

Add-Verdict "failure-reason" "ObligatorySyntacticFeatures" "No" @() (
    "HCLoader never sets AffixProcessRule.ObligatorySyntacticFeatures or CompoundingRule." +
    "ObligatorySyntacticFeatures (checked LoadDerivAffixProcessRule HCLoader.cs:926-974, " +
    "LoadInflAffixProcessRule 976-1006, LoadEndoCompoundingRule 1842-1912, LoadExoCompoundingRule " +
    "1922-2001 -- zero assignments). The only engine trigger, Morpher.cs:603-613, gates on " +
    "word.ObligatorySyntacticFeatures, fed exclusively by those two rule properties, so this " +
    "reason can never fire for a FieldWorks-produced grammar. Searched HCLoader.cs for: " +
    "ObligatorySyntacticFeatures, OutputObligatoryFeatures, obligatoryHeadFeatures, " +
    "obligatoryFootFeatures, Obligatory -- zero hits beyond unrelated identifiers. Matches this " +
    "task's stated known data point exactly, and is the same underlying gap behind interface rows " +
    "CompoundingRule.outputObligatoryFeatures and MorphologicalRule.outputObligatoryFeatures (also No)."
)

Add-Verdict "failure-reason" "AllomorphCoOccurrenceRules" "Yes" @(
    "HCLoader.cs:2163-2189 (LoadAllomorphCoOccurrenceRules)"
    "HCLoader.cs:341-345 (call site, gated on IMoAlloAdhocProhibRepository)"
) "Corroborated by HCLoaderTests.cs:1046-1047 (AllomorphCoOccurrenceRules.Count/First asserted)."

Add-Verdict "failure-reason" "Environments" "Yes" @(
    "HCLoader.cs:815-829 (LoadRootAllomorph)"
    "HCLoader.cs:1322 (LoadCircumfixAffixProcessAllomorph)"
    "HCLoader.cs:1577,1604 (LoadFormAffixProcessAllomorph)"
) "Any phonologically-conditioned allomorph environment a FieldWorks user enters produces this; one of the most routine constructs in the loader."

Add-Verdict "failure-reason" "MorphemeCoOccurrenceRules" "Yes" @(
    "HCLoader.cs:2213-2239 (LoadMorphemeCoOccurrenceRules)"
    "HCLoader.cs:347-351 (call site, gated on IMoMorphAdhocProhibRepository)"
) "Corroborated by HCLoaderTests.cs:1066-1067 (MorphemeCoOccurrenceRules.Count/First asserted)."

Add-Verdict "failure-reason" "DisjunctiveAllomorph" "Yes" @(
    "HCLoader.cs:716-728 (LoadLexEntry, multiple RootAllomorph added per entry)"
    "engine repo Allomorph.cs:127-151 derives this purely from allomorph index/environment ordering"
) "Not gated by a distinct DTD attribute; fires whenever an entry HCLoader builds has more than one ordered, differently-conditioned allomorph -- ordinary FieldWorks allomorphy."

Add-Verdict "failure-reason" "SurfaceFormMismatch" "Yes" @(
    "HCLoader.cs:204,2669-2743 (LoadCharacterDefinitionTable, always invoked once)"
    "HCLoader.cs:233 (Surface stratum, always created)"
) "Intrinsic engine self-check (engine repo Morpher.cs:620-633) comparing a synthesized shape to the surface stratum's table; exercised on every FieldWorks-produced grammar, not gated by an optional construct."

Add-Verdict "failure-reason" "Pattern" "Yes" @(
    "HCLoader.cs:2313-2418 (LoadPatternNode/LoadPatternNodes, used throughout affix and phonological rule construction)"
) "Fires on any Lhs pattern mismatch; reachable trivially for any grammar with at least one rule, which every FieldWorks project produces."

Add-Verdict "failure-reason" "HeadPattern" "Yes" @(
    "HCLoader.cs:1808-1840 (DefaultCompoundingRules, always generated unless NoDefaultCompounding is set)"
    "HCLoader.cs:1878-1904 (LoadEndoCompoundingRule head/nonhead pattern)"
    "HCLoader.cs:1943-1965 (LoadExoCompoundingRule head/nonhead pattern)"
) "Default compounding rules alone make this reachable even with zero user-defined compound rules."

Add-Verdict "failure-reason" "NonHeadPattern" "Yes" @(
    "HCLoader.cs:1808-1840 (DefaultCompoundingRules)"
    "HCLoader.cs:1878-1904,1943-1965 (Endo/Exo nonhead pattern)"
) "Same reachability as HeadPattern."

Add-Verdict "failure-reason" "RequiredSyntacticFeatureStruct" "Yes" @(
    "HCLoader.cs:930-936 (LoadDerivAffixProcessRule)"
    "HCLoader.cs:985-991 (LoadInflAffixProcessRule)"
    "HCLoader.cs:1016-1020 (LoadUnclassifiedAffixProcessRule)"
    "HCLoader.cs:1034-1038 (LoadCliticAffixProcessRule)"
    "HCLoader.cs:2051-2055 (LoadRewriteRule)"
) ""

Add-Verdict "failure-reason" "HeadRequiredSyntacticFeatureStruct" "Yes" @(
    "HCLoader.cs:1844-1892 (LoadEndoCompoundingRule)"
    "HCLoader.cs:1922-1957 (LoadExoCompoundingRule, right rule)"
) ""

Add-Verdict "failure-reason" "NonHeadRequiredSyntacticFeatureStruct" "Yes" @(
    "HCLoader.cs:1844-1892 (LoadEndoCompoundingRule)"
    "HCLoader.cs:1922-1984 (LoadExoCompoundingRule, both rules)"
) ""

Add-Verdict "failure-reason" "HeadProdRestrictMprFeatures" "Yes" @(
    "HCLoader.cs:1888 (LoadEndoCompoundingRule)"
    "HCLoader.cs:1953,1980 (LoadExoCompoundingRule, both rules)"
) "Matches this task's stated known data point (~6 total hits across Head+NonHead assignments)."

Add-Verdict "failure-reason" "NonHeadProdRestrictMprFeatures" "Yes" @(
    "HCLoader.cs:1889 (LoadEndoCompoundingRule)"
    "HCLoader.cs:1954,1981 (LoadExoCompoundingRule, both rules)"
) "Engine trigger site (AnalysisCompoundingRule.cs) is analysis/parse direction only, not synthesis -- still reachable since parsing text is FLEx's primary use of HC."

Add-Verdict "failure-reason" "RequiredMprFeatures" "Yes" @(
    "HCLoader.cs:968,1001,1069,1096,1123 (AffixProcessAllomorph.RequiredMprFeatures)"
    "HCLoader.cs:2057 (RewriteSubrule.RequiredMprFeatures)"
) "The CompoundingSubrule.RequiredMprFeatures path (HeadMorphologicalInput.requiredMPRFeatures) is never populated (see that interface row, No), but the affix/phonological paths alone make this reachable."

Add-Verdict "failure-reason" "ExcludedMprFeatures" "Yes" @(
    "HCLoader.cs:1717 (slot-blocking of irregularly-inflected forms)"
    "HCLoader.cs:2058 (RewriteSubrule.ExcludedMprFeatures)"
) ""

Add-Verdict "failure-reason" "RequiredStemName" "Yes" @(
    "HCLoader.cs:960-962 (LoadDerivAffixProcessRule)"
) "Corroborated by HCLoaderTests.cs:698."

Add-Verdict "failure-reason" "ExcludedStemName" "Yes" @(
    "HCLoader.cs:831-833 (LoadRootAllomorph sets Allomorph.stemName; nothing prevents >1 stem-named allomorph per entry)"
    "engine repo RootAllomorph.cs:72-91 derives ExcludedStemName purely from a second, differently stem-named sibling allomorph"
) "Structural consequence of a stem-name-restricted paradigm with more than one region on one entry's allomorphs -- ordinary FieldWorks usage, same underlying attribute as Allomorph.stemName (Yes)."

Add-Verdict "failure-reason" "PartialParse" "Yes" @(
    "HCLoader.cs:708 (LoadLexEntry, IsPartial when entry has no POS)"
    "HCLoader.cs:982 (LoadInflAffixProcessRule, IsPartial when no slots)"
    "HCLoader.cs:1013 (LoadUnclassifiedAffixProcessRule, always IsPartial)"
) "Corroborated by HCLoaderTests.cs:662,726,845."

Add-Verdict "failure-reason" "BoundRoot" "Yes" @(
    "HCLoader.cs:835-841 (LoadRootAllomorph, IsBound for bound-root/bound-stem morph types)"
) "Corroborated by HCLoaderTests.cs:818."

Add-Verdict "failure-reason" "NonPartialRuleProhibitedAfterFinalTemplate" "Yes" @(
    "HCLoader.cs:1678 (AffixTemplate.IsFinal from LibLCM template.Final)"
    "HCLoader.cs:708,982,1013 (rule IsPartial sites)"
) "Corroborated by HCLoaderTests.cs:750,789 (IsFinal). Reachable whenever a non-final template is followed by a non-partial, non-template rule such as a derivational affix or clitic."

Add-Verdict "failure-reason" "NonPartialRuleRequiredAfterNonFinalTemplate" "Yes" @(
    "HCLoader.cs:1678 (AffixTemplate.IsFinal)"
    "HCLoader.cs:708,982,1013 (rule IsPartial sites)"
) "Same drivers as NonPartialRuleProhibitedAfterFinalTemplate, opposite polarity of the same engine check."

Add-Verdict "failure-reason" "MaxApplicationCount" "Yes" @(
    "HCLoader.cs:103-112 (ParserParameters CompoundRules/maxApps parsed into m_CompoundRuleLookup)"
    "HCLoader.cs:1894-1896 (CompoundingRule.MaxApplicationCount assignment)"
) "Only ever wired for CompoundingRule, not AffixProcessRule (grep-confirmed single assignment site) -- still reachable via the compounding path."

# --- Interface-inventory subjects (subject_kind = interface-attribute) ------

Add-Verdict "interface-attribute" "AffixTemplate.requiredPartsOfSpeech" "Yes" @(
    "HCLoader.cs:1681-1684 (LoadAffixTemplate)"
) ""

Add-Verdict "interface-attribute" "AffixTemplate.requiredSubcategorizedRules" "No" @() (
    "Subcategorization/SyntacticRule is dead schema across the WHOLE engine, not just HCLoader: " +
    "grepping src/SIL.Machine.Morphology.HermitCrab for 'Subcategoriz' outside the DTD file itself " +
    "returns zero hits (no C# class implements it), and HCLoader.cs / HCLoaderTests.cs likewise " +
    "have zero hits for 'Subcategoriz' or 'SyntacticRule'. Confirmed by this repo's own " +
    "InteractionChainLedger.cs comments ('dead (subcategorization)') on all 9 subcategorization-" +
    "family attributes (rows 12,20,23,27,35(no -- separate reason),40,52,55,56,57)."
)

Add-Verdict "interface-attribute" "Allomorph.stemName" "Yes" @(
    "HCLoader.cs:831-833 (LoadRootAllomorph)"
) "Corroborated by HCLoaderTests.cs:817."

Add-Verdict "interface-attribute" "AllomorphCoOccurrenceRule.otherAllomorphs" "Yes" @(
    "HCLoader.cs:2163-2189"
) ""

Add-Verdict "interface-attribute" "AllomorphCoOccurrenceRule.primaryAllomorph" "Yes" @(
    "HCLoader.cs:2166,2181-2186"
) ""

Add-Verdict "interface-attribute" "AlphaVariable.variableFeature" "Yes" @(
    "HCLoader.cs:2005-2011 (LoadRewriteRule, variable naming)"
    "HCLoader.cs:2765-2773 (GetVariables ties a variable name to a specific phonological feature)"
) "The Machine engine has no runtime 'AlphaVariable' class (grep-confirmed) -- the DTD's two-hop AlphaVariable->VariableFeature->feature indirection collapses at runtime to SymbolicFeatureValue(feature, variableName, agree), which HCLoader constructs directly."

Add-Verdict "interface-attribute" "BoundaryMarker.boundary" "Yes" @(
    "HCLoader.cs:2698-2712 (LoadCharacterDefinitionTable loads BoundaryMarkersOC into m_charDefs)"
    "HCLoader.cs:2348-2360 (LoadPatternNode, PhSimpleContextBdry emits a Constraint referencing that charDef)"
) ""

Add-Verdict "interface-attribute" "CompoundingRule.headPartsOfSpeech" "Yes" @(
    "HCLoader.cs:1848-1869 (LoadEndoCompoundingRule)"
    "HCLoader.cs:1924-1931 (LoadExoCompoundingRule)"
) ""

Add-Verdict "interface-attribute" "CompoundingRule.headProdRestrictionsMprFeatures" "Yes" @(
    "HCLoader.cs:1888,1953,1980"
) "Matches this task's stated known data point (~6 hits)."

Add-Verdict "interface-attribute" "CompoundingRule.headSubcategorizedRules" "No" @() "Subcategorization, dead (see AffixTemplate.requiredSubcategorizedRules)."

Add-Verdict "interface-attribute" "CompoundingRule.nonHeadPartsOfSpeech" "Yes" @(
    "HCLoader.cs:1852-1864,1929-1930"
) ""

Add-Verdict "interface-attribute" "CompoundingRule.nonHeadProdRestrictionsMprFeatures" "Yes" @(
    "HCLoader.cs:1889,1954,1981"
) "interface-inventory.tsv shows present=no because no CONFORMANCE FIXTURE happens to exercise it -- a corpus-coverage fact, not an HCLoader-capability fact. The code path is identical to headProdRestrictionsMprFeatures."

Add-Verdict "interface-attribute" "CompoundingRule.nonHeadSubcategorizedRules" "No" @() "Subcategorization, dead."

Add-Verdict "interface-attribute" "CompoundingRule.outputObligatoryFeatures" "No" @() (
    "Absence confirmed across LoadEndoCompoundingRule (HCLoader.cs:1842-1912) and " +
    "LoadExoCompoundingRule (HCLoader.cs:1922-2001) -- neither ever touches " +
    "CompoundingRule.ObligatorySyntacticFeatures. The engine fully implements this " +
    "(CompoundingRule.cs:54-57, XmlLanguageLoader.cs:1227-1231), so this is a FieldWorks-specific " +
    "gap, not dead schema -- the same absence drives FailureReason.ObligatorySyntacticFeatures=No. " +
    "Searched: ObligatorySyntacticFeatures, outputObligatoryFeatures, Obligatory."
)

Add-Verdict "interface-attribute" "CompoundingRule.outputPartOfSpeech" "Yes" @(
    "HCLoader.cs:1873-1876,1932-1935"
) ""

Add-Verdict "interface-attribute" "CompoundingRule.outputProdRestrictionsMprFeatures" "No" @() (
    "Property OutputProdRestrictionsMprFeatures exists on CompoundingRule (engine repo " +
    "CompoundingRule.cs:25,50) and is never set by HCLoader (checked LoadEndoCompoundingRule " +
    "HCLoader.cs:1842-1912 and LoadExoCompoundingRule 1922-2001; grep-confirmed zero hits for " +
    "'OutputProdRestrictionsMprFeatures' across all of HCLoader.cs). Only OutMprFeatures, " +
    "HeadProdRestrictionsMprFeatures and NonHeadProdRestrictionsMprFeatures are ever populated."
)

Add-Verdict "interface-attribute" "CompoundingRule.outputSubcategorization" "No" @() "Subcategorization, dead."

Add-Verdict "interface-attribute" "CopyFromInput.index" "Yes" @(
    "HCLoader.cs:1310,1384,1454-1518,1547-1601 (pervasive)"
) ""

Add-Verdict "interface-attribute" "FeatureValue.feature" "Yes" @(
    "HCLoader.cs:2500-2530 (LoadFeatureStruct)"
) ""

Add-Verdict "interface-attribute" "FeatureValue.symbolValues" "Yes" @(
    "HCLoader.cs:2507-2516"
) ""

Add-Verdict "interface-attribute" "HeadMorphologicalInput.excludedMPRFeatures" "No" @() (
    "Absence confirmed across both compounding-rule loaders (HCLoader.cs:1898-1910 Endo, " +
    "1959-1971/1986-1998 Exo) -- CompoundingSubrule.ExcludedMprFeatures (engine repo " +
    "CompoundingSubrule.cs:48-51) is never set by HCLoader; grep-confirmed only OutMprFeatures is " +
    "ever touched on a CompoundingSubrule. Ground truth for which runtime property this DTD " +
    "attribute maps to: engine repo XmlLanguageLoader.cs:1278-1279."
)

Add-Verdict "interface-attribute" "HeadMorphologicalInput.requiredMPRFeatures" "No" @() (
    "Same absence and citations as HeadMorphologicalInput.excludedMPRFeatures (CompoundingSubrule." +
    "RequiredMprFeatures is likewise never set; XmlLanguageLoader.cs:1278 is the ground-truth mapping)."
)

Add-Verdict "interface-attribute" "InsertSegments.characterDefinitionTable" "No" @() (
    "HCLoader.cs:204 loads exactly one PhonemeSetsOS[0] as the whole grammar's character table; " +
    "HCLoader.cs:2831-2835 (Segments() helper) and every InsertSegments call always route through " +
    "that single m_table field; HCLoader.cs:2742 adds it to m_language.CharacterDefinitionTables " +
    "exactly once. HCLoader architecturally never builds more than one CharacterDefinitionTable, " +
    "so an explicit override to a non-default table -- what this attribute exists for -- can never " +
    "carry a meaningful value, in any FieldWorks project or configuration."
)

Add-Verdict "interface-attribute" "LexicalEntry.family" "No" @() (
    "Full-file grep of HCLoader.cs for 'family'/'Family' returns zero hits. The engine's LexFamily/" +
    "LexEntry.Family mechanism exists (engine repo LexEntry.cs:92, LexFamily.cs) and HCLoader's own " +
    "TODO comment (HCLoader.cs:744, 'irregularly inflected forms should be handled by rule blocking " +
    "in HC') shows it deliberately uses a different, MPR-feature-based mechanism instead of the " +
    "engine's native Family-blocking feature for exactly the case (irregular/variant forms) the DTD " +
    "comment says Family exists for."
)

Add-Verdict "interface-attribute" "LexicalEntry.morphologicalRules" "No" @() (
    "Dead even in the engine's own reference XML loader (this repo's InteractionChainLedger.cs:79, " +
    "'dead: no loader reference at all'). LexEntry has no MorphologicalRules property at all in the " +
    "runtime object model (grep-confirmed against engine repo LexEntry.cs), so HCLoader cannot set " +
    "it regardless of what LibLCM data exists."
)

Add-Verdict "interface-attribute" "LexicalEntry.obligatoryFootFeatures" "No" @() (
    "Dead even in the engine's own reference XML loader (InteractionChainLedger.cs:80). LexEntry " +
    "has no such property (only MprFeatures, SyntacticFeatureStruct -- engine repo LexEntry.cs:80,86)."
)

Add-Verdict "interface-attribute" "LexicalEntry.obligatoryHeadFeatures" "No" @() (
    "Dead even in the engine's own reference XML loader (InteractionChainLedger.cs:81). Matches " +
    "this task's stated expectation. Searched HCLoader.cs and engine repo LexEntry.cs for: " +
    "obligatoryHeadFeatures, ObligatoryHeadFeatures, Obligatory -- zero hits beyond the unrelated " +
    "ObligatorySyntacticFeatures mechanism."
)

Add-Verdict "interface-attribute" "LexicalEntry.partOfSpeech" "Yes" @(
    "HCLoader.cs:705-708"
) ""

Add-Verdict "interface-attribute" "LexicalEntry.ruleFeatures" "Yes" @(
    "HCLoader.cs:695-701,737-746 (inflection-class and productivity-restriction features unioned into hcEntry.MprFeatures)"
) ""

Add-Verdict "interface-attribute" "LexicalEntry.subcategorizations" "No" @() "Subcategorization, dead."

Add-Verdict "interface-attribute" "MetathesisRule.leftSwitch" "Yes" @(
    "HCLoader.cs:2103-2161 (LoadMetathesisRule)"
    "HCLoader.cs:322-336 (call site, gated on LeftSwitchIndex/RightSwitchIndex != -1)"
) ""

Add-Verdict "interface-attribute" "MetathesisRule.rightSwitch" "Yes" @(
    "HCLoader.cs:2103-2161"
    "HCLoader.cs:322-336"
) ""

Add-Verdict "interface-attribute" "ModifyFromInput.index" "Yes" @(
    "HCLoader.cs:1409-1419"
) ""

Add-Verdict "interface-attribute" "MorphemeCoOccurrenceRule.otherMorphemes" "Yes" @(
    "HCLoader.cs:2213-2239"
) ""

Add-Verdict "interface-attribute" "MorphemeCoOccurrenceRule.primaryMorpheme" "Yes" @(
    "HCLoader.cs:2215-2235"
) ""

Add-Verdict "interface-attribute" "MorphologicalInput.excludedMPRFeatures" "Yes" @(
    "HCLoader.cs:1717 (AffixProcessAllomorph.ExcludedMprFeatures)"
) "Ground-truth mapping via engine repo XmlLanguageLoader.cs:1057-1062."

Add-Verdict "interface-attribute" "MorphologicalInput.requiredMPRFeatures" "Yes" @(
    "HCLoader.cs:968,1001,1069,1096,1123"
) ""

Add-Verdict "interface-attribute" "MorphologicalOutput.MPRFeatures" "Yes" @(
    "HCLoader.cs:969 (AffixProcessAllomorph.OutMprFeatures)"
    "HCLoader.cs:1901,1961,1988 (CompoundingSubrule.OutMprFeatures)"
) ""

Add-Verdict "interface-attribute" "MorphologicalPhonologicalRuleFeatureGroup.features" "Yes" @(
    "HCLoader.cs:168-192 (LoadLanguage, three MprFeatureGroups)"
    "HCLoader.cs:571-577 (LoadMprFeature, group.MprFeatures.Add(feat))"
) ""

Add-Verdict "interface-attribute" "MorphologicalRule.outputObligatoryFeatures" "No" @() (
    "Absence confirmed across all four AffixProcessRule loaders (HCLoader.cs:926-1046). " +
    "AffixProcessRule.ObligatorySyntacticFeatures (engine repo AffixProcessRule.cs:68) is never " +
    "set. Same underlying gap as CompoundingRule.outputObligatoryFeatures and " +
    "FailureReason.ObligatorySyntacticFeatures (both also No)."
)

Add-Verdict "interface-attribute" "MorphologicalRule.outputPartOfSpeech" "Yes" @(
    "HCLoader.cs:938-944"
) ""

Add-Verdict "interface-attribute" "MorphologicalRule.outputSubcategorization" "No" @() "Subcategorization, dead."

Add-Verdict "interface-attribute" "MorphologicalRule.requiredPartsOfSpeech" "Yes" @(
    "HCLoader.cs:930-936,985-991,1016-1020,1034-1038"
) ""

Add-Verdict "interface-attribute" "MorphologicalRule.requiredStemName" "Yes" @(
    "HCLoader.cs:960-962"
) "Corroborated by HCLoaderTests.cs:698."

Add-Verdict "interface-attribute" "MorphologicalRule.requiredSubcategorizedRules" "No" @() "Subcategorization, dead."

Add-Verdict "interface-attribute" "OutputSubcategorizationOverride.inputSubcategorization" "No" @() "Subcategorization, dead; the OutputSubcategorizationOverride element is never referenced anywhere in HCLoader.cs."

Add-Verdict "interface-attribute" "OutputSubcategorizationOverride.outputSubcategorization" "No" @() "Subcategorization, dead; see inputSubcategorization."

Add-Verdict "interface-attribute" "PhonologicalSubrule.excludedMPRFeatures" "Yes" @(
    "HCLoader.cs:2058"
) "interface-inventory.tsv shows present=no (no fixture exercises it) but the HCLoader code path is live -- a corpus-coverage fact, not a capability fact."

Add-Verdict "interface-attribute" "PhonologicalSubrule.requiredMPRFeatures" "Yes" @(
    "HCLoader.cs:2057"
) "Same corpus-coverage caveat as excludedMPRFeatures."

Add-Verdict "interface-attribute" "PhonologicalSubrule.requiredPartsOfSpeech" "Yes" @(
    "HCLoader.cs:2051-2055"
) ""

Add-Verdict "interface-attribute" "Segment.segment" "Yes" @(
    "HCLoader.cs:2362-2374 (LoadPatternNode, PhSimpleContextSeg)"
) ""

Add-Verdict "interface-attribute" "Segments.characterDefinitionTable" "No" @() (
    "Same reasoning and citations as InsertSegments.characterDefinitionTable: HCLoader always " +
    "builds exactly one CharacterDefinitionTable (HCLoader.cs:204,2742) so a per-instance override " +
    "to a different table can never carry a meaningful value."
)

Add-Verdict "interface-attribute" "SimpleContext.naturalClass" "Yes" @(
    "HCLoader.cs:2745-2786 (TryLoadSimpleContext, both overloads)"
) ""

Add-Verdict "interface-attribute" "Slot.morphologicalRules" "Yes" @(
    "HCLoader.cs:1702-1731 (LoadAffixTemplate slot/rule assembly)"
) ""

Add-Verdict "interface-attribute" "StemName.partsOfSpeech" "Yes" @(
    "HCLoader.cs:206-224 (LoadLanguage, StemName regions)"
) ""

Add-Verdict "interface-attribute" "Stratum.characterDefinitionTable" "Yes" @(
    "HCLoader.cs:227-233 (default strata)"
    "HCLoader.cs:374 (CreateStrata, custom strata)"
) ""

Add-Verdict "interface-attribute" "Stratum.morphologicalRules" "Yes" @(
    "HCLoader.cs:237,246,250 (compounding rules)"
    "HCLoader.cs:290-292,916-923 (AddMorphologicalRule)"
) ""

Add-Verdict "interface-attribute" "Stratum.phonologicalRules" "Yes" @(
    "HCLoader.cs:314-333"
) ""

Add-Verdict "interface-attribute" "SymbolicFeature.defaultSymbol" "No" @() (
    "HCLoader.cs:2650-2667 (LoadFeatureSystem) never sets a default value on any SymbolicFeature " +
    "it builds. The engine property is SymbolicFeature.DefaultSymbolID/DefaultValue (engine repo " +
    "FeatureModel/SymbolicFeature.cs:34-36), read by XmlLanguageLoader.cs:644 -- so this is a " +
    "FieldWorks-specific gap, not dead schema. Searched HCLoader.cs for: Default, DefaultSymbol, " +
    "DefaultValue -- only unrelated hits (writing-system/inflection-class/compounding defaults)."
)

Add-Verdict "interface-attribute" "VariableFeature.phonologicalFeature" "Yes" @(
    "HCLoader.cs:2765-2773 (GetVariables)"
) "Same runtime-collapse caveat as AlphaVariable.variableFeature -- no 'VariableFeature' class exists in the engine's runtime object model (grep-confirmed), so this is evaluated via the equivalent runtime construct HCLoader actually builds."

# ---------------------------------------------------------------------------
# 4. Join mechanically-extracted subjects against the verdict table. Any
#    subject missing a verdict is a loud failure, never a silent default.
# ---------------------------------------------------------------------------
$rows = New-Object System.Collections.Generic.List[PSCustomObject]

foreach ($reason in $failureReasons) {
    $key = "failure-reason|$reason"
    if (-not $Verdicts.ContainsKey($key)) { throw "No verdict recorded for FailureReason.$reason -- add one to `$Verdicts before regenerating." }
    $v = $Verdicts[$key]
    $rows.Add([PSCustomObject]@{
        subject_kind   = "failure-reason"
        subject        = $reason
        producible     = $v.Producible
        hcloader_sites = $v.Sites
        notes          = $v.Notes
    })
}

foreach ($attr in $interfaceAttrs) {
    $subject = "$($attr.Element).$($attr.Attribute)"
    $key = "interface-attribute|$subject"
    if (-not $Verdicts.ContainsKey($key)) { throw "No verdict recorded for interface attribute $subject -- add one to `$Verdicts before regenerating." }
    $v = $Verdicts[$key]
    $rows.Add([PSCustomObject]@{
        subject_kind   = "interface-attribute"
        subject        = $subject
        producible     = $v.Producible
        hcloader_sites = $v.Sites
        notes          = $v.Notes
    })
}

# Verify every verdict was actually consumed (catches a stale entry after a rename/removal).
$expectedKeys = @()
$expectedKeys += $failureReasons | ForEach-Object { "failure-reason|$_" }
$expectedKeys += $interfaceAttrs | ForEach-Object { "interface-attribute|$($_.Element).$($_.Attribute)" }
$staleKeys = $Verdicts.Keys | Where-Object { $expectedKeys -notcontains $_ }
if ($staleKeys) { throw "Verdict table has entries with no matching subject (stale after a rename?): $($staleKeys -join ', ')" }

# Uniqueness, mechanically enforced.
$dupes = $rows | Group-Object subject_kind, subject | Where-Object { $_.Count -gt 1 }
if ($dupes) { throw "Duplicate subjects in output: $($dupes.Name -join '; ')" }

if ($rows.Count -ne 83) { throw "Expected 83 total rows (23 failure-reason + 60 interface-attribute), got $($rows.Count)" }

# ---------------------------------------------------------------------------
# 5. Emit the TSV.
# ---------------------------------------------------------------------------
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# GENERATED by conformance/tools/generate-fieldworks-producibility.ps1. One row per subject,")
$lines.Add("# where subjects are every SIL.Machine.Morphology.HermitCrab.FailureReason enum member (except")
$lines.Add("# None) plus every (element, attribute) row in conformance/interface-inventory.tsv. Authority:")
$lines.Add("# FieldWorks' HCLoader.cs (Src/LexText/ParserCore/HCLoader.cs) -- the component that turns a")
$lines.Add("# real FieldWorks/LibLCM project into an HC grammar. producible=No means no FieldWorks user can")
$lines.Add("# ever produce this construct, so covering it elsewhere in this suite proves nothing about real")
$lines.Add("# use. See conformance/docs/how-it-is-computed.md for the full explanation, including why this")
$lines.Add("# ledger is a point-in-time snapshot of an external repo that cannot be drift-checked here.")
$lines.Add("subject_kind`tsubject`tproducible`thcloader_sites`tnotes")
foreach ($row in $rows) {
    $lines.Add(("{0}`t{1}`t{2}`t{3}`t{4}" -f $row.subject_kind, $row.subject, $row.producible, $row.hcloader_sites, $row.notes))
}

Set-Content -Path $OutputPath -Value $lines -Encoding utf8NoBOM
Write-Host "Wrote $($rows.Count) rows to $OutputPath"
$yes = @($rows | Where-Object { $_.producible -eq "Yes" }).Count
$no = @($rows | Where-Object { $_.producible -eq "No" }).Count
$cond = @($rows | Where-Object { $_.producible -eq "Conditional" }).Count
Write-Host "producible: Yes=$yes No=$no Conditional=$cond"
