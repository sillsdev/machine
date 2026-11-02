using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// A fixture flagged <c>fieldworks_producible: true</c> must not use a grammar shape HCLoader never
/// writes. The flag is authored, so these checks can only prove it wrong. Most of the checks come from the
/// census table <c>conformance/fieldworks-writable-shapes.tsv</c>, which says per rule kind which DTD members
/// HCLoader writes; <see cref="FieldWorksWritableShapes"/> reads it. The checks written here are the ones a
/// table row cannot say: a <c>linear</c> stratum that holds rules or templates, and a partial stem entry
/// with a part of speech. Each class of violation has one test that must fail on a synthetic grammar.
/// </summary>
[TestFixture]
public sealed class FieldWorksProducibleShapeTests
{
    private static readonly Lazy<Dictionary<(string Kind, string Member), string>> LoadedTable = new(() =>
        FieldWorksWritableShapes.LoadTable(FieldWorksWritableShapes.TablePath(ConformanceSuite.Root()))
    );

    private static Dictionary<(string Kind, string Member), string> Table => LoadedTable.Value;

    // The 16 stress fixtures sit in the suite under stress/ and obey the same FieldWorks-producible rule.
    private static List<Fixture> DiscoverProducible()
    {
        string root = ConformanceSuite.Root();
        var fixtures = Fixture.DiscoverAll(root);
        fixtures.AddRange(Fixture.DiscoverAll(Path.Combine(root, "stress")));
        return fixtures.Where(fixture => fixture.Words.FieldworksProducible == true).ToList();
    }

    [Test]
    public void FlaggedFixturesUseNoShapeHcLoaderCannotWrite()
    {
        List<Fixture> fixtures = DiscoverProducible();
        Assert.That(fixtures, Has.Count.GreaterThanOrEqualTo(25));

        string[] offences = fixtures
            .SelectMany(fixture => FindUnproducibleShapes(fixture.GrammarPath).Select(o => $"{fixture.Id}: {o}"))
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            offences,
            Is.Empty,
            "these fixtures are flagged fieldworks_producible but use a shape HCLoader never writes; "
                + "rewrite the grammar and re-run the oracle, or delete the fixture:\n  "
                + string.Join("\n  ", offences)
        );
    }

    [Test]
    public void TheCensusTableNamesOnlyMembersTheDtdDeclares()
    {
        string dtd = File.ReadAllText(Path.Combine(ConformanceSuite.Root(), "HermitCrabInput.dtd"));
        var elements = new HashSet<string>(
            Regex.Matches(dtd, @"<!ELEMENT\s+(\w+)").Select(m => m.Groups[1].Value),
            StringComparer.Ordinal
        );
        var attributes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match list in Regex.Matches(dtd, @"<!ATTLIST\s+\w+(.*?)>", RegexOptions.Singleline))
        {
            foreach (Match attribute in Regex.Matches(list.Groups[1].Value, @"^\s*(\w+)\s", RegexOptions.Multiline))
                attributes.Add(attribute.Groups[1].Value);
        }

        string[] unknown = Table
            .Keys.Select(key => key.Member)
            .Distinct(StringComparer.Ordinal)
            .Where(member => member != "*")
            .Where(member =>
                member.StartsWith('@')
                    ? !attributes.Contains(member.TrimStart('@').Split('=')[0])
                    : !elements.Contains(member)
            )
            .OrderBy(member => member, StringComparer.Ordinal)
            .ToArray();

        Assert.That(unknown, Is.Empty, "the census table names members the DTD does not declare");
    }

    [Test]
    public void TheCensusTableGivesALoaderLineToEveryRowThatSaysHcLoaderWritesIt()
    {
        string[] rows = File.ReadAllLines(FieldWorksWritableShapes.TablePath(ConformanceSuite.Root()))
            .Skip(1)
            .ToArray();
        string[] missing = rows.Select(row => row.Split('\t'))
            .Where(cells => cells[2] is "writes" or "never" or "omitted" && cells[3].Length == 0)
            .Select(cells => $"{cells[0]} {cells[1]}")
            .OrderBy(row => row, StringComparer.Ordinal)
            .ToArray();

        Assert.That(missing, Is.Empty, "census rows without a HCLoader.cs line");
    }

    [Test]
    public void TheCheckFindsAMemberTheCensusTableDoesNotList()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions><MorphologicalRule id="mrNew" newAttribute="x" /></MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(new[] { "derivational-rule mrNew uses @newAttribute, which the census table does not list" })
        );
    }

    [Test]
    public void TheCheckFindsARealizationalRule()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions><RealizationalRule id="rrOne" /></MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(new[] { "realizational-rule rrOne is never written by HCLoader (HCLoader.cs 1027)" })
        );
    }

    [Test]
    public void TheCheckFindsASlotRuleWithAnOutputPartOfSpeech()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrSlot" outputPartOfSpeech="posV" />
                <MorphologicalRule id="mrLoose" outputPartOfSpeech="posV" />
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlot" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(new[] { "slot-rule mrSlot uses @outputPartOfSpeech, which HCLoader never writes" })
        );
    }

    [Test]
    public void TheCheckFindsOutputFeaturesOutputMprAndARequiredStemNameOnASlotRule()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrSlot" requiredStemName="sn1">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="sub1">
                      <MorphologicalInput />
                      <MorphologicalOutput MPRFeatures="mprA" />
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                  <OutputHeadFeatures><FeatureValue feature="hf" symbolValues="a" /></OutputHeadFeatures>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlot" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "slot-rule mrSlot uses @requiredStemName, which HCLoader never writes",
                    "slot-rule mrSlot uses OutputHeadFeatures, which HCLoader never writes",
                    "slot-rule/output sub1 uses @MPRFeatures, which HCLoader never writes",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsHeadFeatureRequirementsOnACompoundingRule()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <CompoundingRule id="crHead">
                  <HeadRequiredHeadFeatures><FeatureValue feature="hf" symbolValues="a" /></HeadRequiredHeadFeatures>
                  <NonHeadRequiredHeadFeatures><FeatureValue feature="hf" symbolValues="b" /></NonHeadRequiredHeadFeatures>
                </CompoundingRule>
                <CompoundingRule id="crPlain" />
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "compounding-rule crHead uses HeadRequiredHeadFeatures, which HCLoader never writes",
                    "compounding-rule crHead uses NonHeadRequiredHeadFeatures, which HCLoader never writes",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsAnEmptyStemNameRegion()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered" />
            """,
            """
            <StemNames>
              <StemName id="snEmpty" partsOfSpeech="posN"><Name>e</Name><Regions><Region /></Regions></StemName>
              <StemName id="snNoFeatures" partsOfSpeech="posN">
                <Name>f</Name><Regions><Region><AssignedHeadFeatures /></Region></Regions>
              </StemName>
              <StemName id="snOk" partsOfSpeech="posN">
                <Name>g</Name>
                <Regions>
                  <Region><AssignedHeadFeatures><FeatureValue feature="hf" symbolValues="a" /></AssignedHeadFeatures></Region>
                </Regions>
              </StemName>
            </StemNames>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "stem name snEmpty has an empty region, which HCLoader skips (HCLoader.cs 223)",
                    "stem name snNoFeatures has an empty region, which HCLoader skips (HCLoader.cs 223)",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsARequiredEnvironmentOnASubruleShapeHcLoaderGivesNone()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrShapes">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="infix">
                      <MorphologicalOutput>
                        <CopyFromInput index="a" /><InsertSegments /><CopyFromInput index="b" />
                      </MorphologicalOutput>
                      <RequiredEnvironments><Environment><LeftEnvironment /></Environment></RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="process">
                      <MorphologicalOutput><CopyFromInput index="a" /><ModifyFromInput index="b" /></MorphologicalOutput>
                      <RequiredEnvironments><Environment><RightEnvironment /></Environment></RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="infixNoEnvironment">
                      <MorphologicalOutput>
                        <CopyFromInput index="a" /><InsertSegments /><CopyFromInput index="b" />
                      </MorphologicalOutput>
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "subrule infix has a RequiredEnvironments on an output of CopyFromInput, InsertSegments, "
                        + "CopyFromInput; HCLoader writes an environment only for a lone prefix, a lone suffix or a circumfix",
                    "subrule process has a RequiredEnvironments on an output of CopyFromInput, ModifyFromInput; "
                        + "HCLoader writes an environment only for a lone prefix, a lone suffix or a circumfix",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsRequiredHeadFeaturesOnASubruleShapeHcLoaderGivesNone()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrShapes">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="circumfix">
                      <MorphologicalOutput><InsertSegments /><CopyFromInput index="a" /><InsertSegments /></MorphologicalOutput>
                      <RequiredHeadFeatures />
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="process">
                      <MorphologicalOutput><CopyFromInput index="a" /><ModifyFromInput index="b" /></MorphologicalOutput>
                      <RequiredHeadFeatures />
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="prefix">
                      <MorphologicalOutput><InsertSegments /><CopyFromInput index="a" /></MorphologicalOutput>
                      <RequiredHeadFeatures />
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="infix">
                      <MorphologicalOutput><CopyFromInput index="a" /><InsertSegments /><CopyFromInput index="b" /></MorphologicalOutput>
                      <RequiredHeadFeatures />
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="circumfixNoFeatures">
                      <MorphologicalOutput><InsertSegments /><CopyFromInput index="a" /><InsertSegments /></MorphologicalOutput>
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "subrule circumfix is a circumfix with RequiredHeadFeatures; HCLoader writes it only on a lone affix form",
                    "subrule process is an affix process with RequiredHeadFeatures; HCLoader writes it only on a lone affix form",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsRequiredHeadFeaturesOnACliticSubrule()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered" morphologicalRules="mrClitic">
              <Name>Clitics</Name>
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrClitic">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="enclitic">
                      <MorphologicalOutput><CopyFromInput index="a" /><InsertSegments /></MorphologicalOutput>
                      <RequiredHeadFeatures />
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(new[] { "clitic-rule/subrule enclitic uses RequiredHeadFeatures, which HCLoader never writes" })
        );
    }

    [Test]
    public void TheCheckFindsACapOnAnExocompound()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <CompoundingRule id="exoLeft" multipleApplication="3" headPartsOfSpeech="posN"
                                 nonHeadPartsOfSpeech="posV" outputPartOfSpeech="posN"><Name>exo</Name></CompoundingRule>
                <CompoundingRule id="exoRight" headPartsOfSpeech="posV"
                                 nonHeadPartsOfSpeech="posN" outputPartOfSpeech="posN"><Name>exo</Name></CompoundingRule>
                <CompoundingRule id="endo" multipleApplication="3" headPartsOfSpeech="posN"
                                 nonHeadPartsOfSpeech="posN" outputPartOfSpeech="posN"><Name>endo</Name></CompoundingRule>
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "compounding rule exoLeft is half of an exocompound, which HCLoader never gives a cap; "
                        + "it has multipleApplication=\"3\"",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsMprFeaturesHcLoaderCannotPlace()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrSlot">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="subSlot">
                      <MorphologicalInput excludedMPRFeatures="mprExc" />
                      <MorphologicalOutput />
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
                <MorphologicalRule id="mrDeriv">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="subA">
                      <MorphologicalInput requiredMPRFeatures="mprInfl" /><MorphologicalOutput MPRFeatures="mprExc" />
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="subB">
                      <MorphologicalInput excludedMPRFeatures="mprExc" /><MorphologicalOutput MPRFeatures="mprExc" />
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlot" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """,
            """
            <MorphologicalPhonologicalRuleFeatures>
              <MorphologicalPhonologicalRuleFeature id="mprInfl" />
              <MorphologicalPhonologicalRuleFeature id="mprExc" />
              <MorphologicalPhonologicalRuleFeature id="mprLoose" />
              <MorphologicalPhonologicalRuleFeatureGroup features="mprInfl" matchType="all" outputType="append">
                <Name>inflClasses</Name>
              </MorphologicalPhonologicalRuleFeatureGroup>
              <MorphologicalPhonologicalRuleFeatureGroup features="mprExc" matchType="all"><Name>exceptionFeatures</Name>
              </MorphologicalPhonologicalRuleFeatureGroup>
              <MorphologicalPhonologicalRuleFeatureGroup features="mprExc"><Name>custom</Name>
              </MorphologicalPhonologicalRuleFeatureGroup>
            </MorphologicalPhonologicalRuleFeatures>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "MPR feature group custom is not one of the three groups HCLoader builds",
                    "MPR feature group inflClasses has matchType=\"all\"; HCLoader builds \"any\"",
                    "MPR feature group inflClasses has outputType=\"append\", which HCLoader never sets",
                    "MPR feature mprLoose is in no group; HCLoader puts every MPR feature in one of three groups",
                    "derivational rule mrDeriv has different MPR lists on its subrules; "
                        + "HCLoader gives every allomorph the same list (HCLoader.cs 1015-1018)",
                    "derivational-rule/input subB uses @excludedMPRFeatures, which HCLoader never writes",
                    "rule mrDeriv excludes MPR feature mprExc; HCLoader excludes only the inflection-type "
                        + "features of a slot (HCLoader.cs 1766)",
                    "rule mrSlot excludes MPR feature mprExc; HCLoader excludes only the inflection-type "
                        + "features of a slot (HCLoader.cs 1766)",
                }
            )
        );
    }

    [Test]
    public void TheCheckAcceptsAnInflectionTypeFeatureExcludedByASlotRule()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrSlot">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="subSlot">
                      <MorphologicalInput excludedMPRFeatures="mprType" />
                      <MorphologicalOutput><InsertSegments /><CopyFromInput index="s" /></MorphologicalOutput>
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlot" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """,
            """
            <MorphologicalPhonologicalRuleFeatures>
              <MorphologicalPhonologicalRuleFeature id="mprType" />
              <MorphologicalPhonologicalRuleFeatureGroup features="mprType" matchType="all">
                <Name>lexEntryInflTypes</Name>
              </MorphologicalPhonologicalRuleFeatureGroup>
            </MorphologicalPhonologicalRuleFeatures>
            """
        );

        Assert.That(found, Is.Empty);
    }

    [Test]
    public void TheCheckAcceptsAttributesHcLoaderOmitsOrNeverReads()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered" cyclicity="cyclic" phonologicalRuleOrder="simultaneous">
              <MorphologicalRuleDefinitions>
                <CompoundingRule id="crOff" isActive="no" />
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate isActive="no"><Slot morphologicalRules="crOff" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """
        );

        Assert.That(found, Is.Empty);
    }

    [Test]
    public void TheCheckFindsALinearStratumThatHoldsAMorphologicalRuleOrATemplate()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRules="mrA">
              <MorphologicalRuleDefinitions><MorphologicalRule id="mrA" /></MorphologicalRuleDefinitions>
            </Stratum>
            <Stratum morphologicalRuleOrder="linear" morphologicalRules="mrB">
              <MorphologicalRuleDefinitions><CompoundingRule id="mrB" /></MorphologicalRuleDefinitions>
            </Stratum>
            <Stratum morphologicalRuleOrder="linear">
              <MorphologicalRuleDefinitions><MorphologicalRule id="mrSlotOnly" /></MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlotOnly" /></AffixTemplate></AffixTemplates>
            </Stratum>
            <Stratum morphologicalRuleOrder="unordered">
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlotOnly" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "stratum #1 is linear (the DTD default) but holds morphological rules mrA",
                    "stratum #2 is linear but holds morphological rules mrB",
                    "stratum #3 is linear but holds 1 affix template(s)",
                }
            )
        );
    }

    [Test]
    public void TheCheckAllowsLinearStrataThatHoldNoActiveRuleOrTemplate()
    {
        string[] found = Scan(
            """
            <Stratum>
              <MorphologicalRuleDefinitions>
                <CompoundingRule id="crInactive" isActive="no" />
              </MorphologicalRuleDefinitions>
              <AffixTemplates>
                <AffixTemplate isActive="no"><Slot morphologicalRules="crInactive" /></AffixTemplate>
              </AffixTemplates>
            </Stratum>
            <Stratum morphologicalRules="crInactive">
              <MorphologicalRuleDefinitions>
                <CompoundingRule id="crInactive" isActive="no" />
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(found, Is.Empty);
    }

    [Test]
    public void TheCheckFindsPartialShapesHcLoaderCannotWrite()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered" morphologicalRules="mrLoose">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrSlot" partial="true" />
                <MorphologicalRule id="mrLoose" partial="true" outputPartOfSpeech="posP" />
                <MorphologicalRule id="mrOk" partial="true" />
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlot" /></AffixTemplate></AffixTemplates>
              <LexicalEntries>
                <LexicalEntry id="eBad" partOfSpeech="posN" partial="true" />
                <LexicalEntry id="eOk" partial="true" />
              </LexicalEntries>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "partial stem entry eBad has a partOfSpeech",
                    "slot-rule mrSlot uses @partial=true, which HCLoader never writes",
                    "unslotted-rule mrLoose uses @outputPartOfSpeech, which HCLoader never writes",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsFootFeatures()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrFoot">
                  <MorphologicalSubrules />
                  <OutputFootFeatures><FeatureValue feature="ff" symbolValues="a" /></OutputFootFeatures>
                  <RequiredFootFeatures><FeatureValue feature="ff" symbolValues="a" /></RequiredFootFeatures>
                </MorphologicalRule>
                <CompoundingRule id="crFoot">
                  <HeadRequiredFootFeatures><FeatureValue feature="ff" symbolValues="a" /></HeadRequiredFootFeatures>
                </CompoundingRule>
              </MorphologicalRuleDefinitions>
              <LexicalEntries>
                <LexicalEntry id="eFoot"><AssignedFootFeatures><FeatureValue feature="ff" symbolValues="a" /></AssignedFootFeatures></LexicalEntry>
                <LexicalEntry id="eHead"><AssignedHeadFeatures><FeatureValue feature="hf" symbolValues="a" /></AssignedHeadFeatures></LexicalEntry>
              </LexicalEntries>
            </Stratum>
            """,
            "<FootFeatures><SymbolicFeature id=\"ff\" /></FootFeatures>"
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "compounding-rule crFoot uses HeadRequiredFootFeatures, which HCLoader never writes",
                    "derivational-rule mrFoot uses OutputFootFeatures, which HCLoader never writes",
                    "derivational-rule mrFoot uses RequiredFootFeatures, which HCLoader never writes",
                    "lexical-entry eFoot uses AssignedFootFeatures, which HCLoader never writes",
                    "the language defines FootFeatures, which HCLoader never writes",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsExcludedEnvironments()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrOk">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="subOk">
                      <MorphologicalOutput><InsertSegments /><CopyFromInput index="s" /></MorphologicalOutput>
                      <RequiredEnvironments><Environment><LeftEnvironment /></Environment></RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="subExcl">
                      <ExcludedEnvironments><Environment><LeftEnvironment /></Environment></ExcludedEnvironments>
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
              <LexicalEntries>
                <LexicalEntry id="eExcl">
                  <Allomorphs>
                    <Allomorph id="aExcl"><ExcludedEnvironments><Environment /></ExcludedEnvironments></Allomorph>
                  </Allomorphs>
                </LexicalEntry>
              </LexicalEntries>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "derivational-rule/subrule subExcl uses ExcludedEnvironments, which HCLoader never writes",
                    "root-allomorph aExcl uses ExcludedEnvironments, which HCLoader never writes",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsARuleThatIsNotBlockable()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrNo" blockable="false" />
                <MorphologicalRule id="mrYes" blockable="true" />
                <MorphologicalRule id="mrDefault" />
                <CompoundingRule id="crNo" blockable="false" />
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "compounding-rule crNo uses @blockable=false, which HCLoader never writes",
                    "derivational-rule mrNo uses @blockable=false, which HCLoader never writes",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsAWrongSideEnvironmentOnALonePrefixOrSuffix()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrAffixes">
                  <MorphologicalSubrules>
                    <MorphologicalSubrule id="prefixLeftOk">
                      <MorphologicalOutput><InsertSegments /><CopyFromInput index="s" /></MorphologicalOutput>
                      <RequiredEnvironments><Environment><LeftEnvironment /></Environment></RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="prefixRightBad">
                      <MorphologicalOutput><InsertSegments /><CopyFromInput index="s" /></MorphologicalOutput>
                      <RequiredEnvironments><Environment><RightEnvironment /></Environment></RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="suffixRightOk">
                      <MorphologicalOutput><CopyFromInput index="s" /><InsertSegments /></MorphologicalOutput>
                      <RequiredEnvironments><Environment><RightEnvironment /></Environment></RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="suffixLeftBad">
                      <MorphologicalOutput><CopyFromInput index="s" /><InsertSegments /></MorphologicalOutput>
                      <RequiredEnvironments><Environment><LeftEnvironment /></Environment></RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="suffixBothBad">
                      <MorphologicalOutput><CopyFromInput index="s" /><InsertSegments /></MorphologicalOutput>
                      <RequiredEnvironments>
                        <Environment><LeftEnvironment /><RightEnvironment /></Environment>
                      </RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="circumfixBothOk">
                      <MorphologicalOutput>
                        <InsertSegments /><CopyFromInput index="s" /><InsertSegments />
                      </MorphologicalOutput>
                      <RequiredEnvironments>
                        <Environment><LeftEnvironment /><RightEnvironment /></Environment>
                      </RequiredEnvironments>
                    </MorphologicalSubrule>
                    <MorphologicalSubrule id="noEnvironment">
                      <MorphologicalOutput><CopyFromInput index="s" /><InsertSegments /></MorphologicalOutput>
                    </MorphologicalSubrule>
                  </MorphologicalSubrules>
                </MorphologicalRule>
              </MorphologicalRuleDefinitions>
              <LexicalEntries>
                <LexicalEntry id="eRoot">
                  <Allomorphs>
                    <Allomorph id="aRoot">
                      <RequiredEnvironments>
                        <Environment><LeftEnvironment /><RightEnvironment /></Environment>
                      </RequiredEnvironments>
                    </Allomorph>
                  </Allomorphs>
                </LexicalEntry>
              </LexicalEntries>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "subrule prefixRightBad is a lone prefix with a right environment; HCLoader writes only a left one",
                    "subrule suffixBothBad is a lone suffix with a left environment; HCLoader writes only a right one",
                    "subrule suffixLeftBad is a lone suffix with a left environment; HCLoader writes only a right one",
                }
            )
        );
    }

    [Test]
    public void TheCheckFindsAMaximumApplicationCountHcLoaderCannotSet()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrOnce" multipleApplication="1" />
                <MorphologicalRule id="mrTwice" multipleApplication="2" />
                <CompoundingRule id="crZero" multipleApplication="0" />
                <CompoundingRule id="crNine" multipleApplication="9" />
                <CompoundingRule id="crDefault" />
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "compounding-rule crZero uses @multipleApplication=0, which HCLoader never writes",
                    "derivational-rule mrTwice uses @multipleApplication=2, which HCLoader never writes",
                }
            )
        );
    }

    private static string[] Scan(string strata, string languageExtras = "")
    {
        string path = Path.Combine(Path.GetTempPath(), $"hc-shape-{Guid.NewGuid():N}.xml");
        File.WriteAllText(
            path,
            $"<HermitCrabInput><Language>{languageExtras}<Strata>{strata}</Strata></Language></HermitCrabInput>"
        );
        try
        {
            return FindUnproducibleShapes(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static readonly string[] RuleElements = ["MorphologicalRule", "RealizationalRule", "CompoundingRule"];

    private static string[] FindUnproducibleShapes(string grammarPath)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using XmlReader reader = XmlReader.Create(grammarPath, settings);
        XDocument doc = XDocument.Load(reader);

        List<string> offences = FieldWorksWritableShapes.Check(doc, Table);

        foreach (XElement entry in doc.Descendants("LexicalEntry"))
        {
            if ((string?)entry.Attribute("partial") == "true" && entry.Attribute("partOfSpeech") is not null)
                offences.Add($"partial stem entry {(string?)entry.Attribute("id")} has a partOfSpeech");
        }

        // HCLoader builds every stratum that holds rules or templates as Unordered, and the DTD default is
        // linear. The engine reads the order for the listed rules and when it applies templates.
        int number = 0;
        foreach (XElement stratum in doc.Descendants("Stratum"))
        {
            number++;
            string? order = (string?)stratum.Attribute("morphologicalRuleOrder");
            if (order == "unordered")
                continue;

            var active = new HashSet<string>(StringComparer.Ordinal);
            foreach (
                XElement rule in stratum
                    .Elements("MorphologicalRuleDefinitions")
                    .Elements()
                    .Where(e => RuleElements.Contains(e.Name.LocalName))
            )
            {
                string? id = (string?)rule.Attribute("id");
                if (id is not null && (string?)rule.Attribute("isActive") != "no")
                    active.Add(id);
            }

            string how = order is null ? "linear (the DTD default)" : "linear";
            string[] held = Split((string?)stratum.Attribute("morphologicalRules")).Where(active.Contains).ToArray();
            if (held.Length > 0)
                offences.Add($"stratum #{number} is {how} but holds morphological rules {string.Join(" ", held)}");

            int templates = stratum
                .Elements("AffixTemplates")
                .Elements("AffixTemplate")
                .Count(template => (string?)template.Attribute("isActive") != "no");
            if (templates > 0)
                offences.Add($"stratum #{number} is {how} but holds {templates} affix template(s)");
        }

        return offences.OrderBy(line => line, StringComparer.Ordinal).ToArray();
    }

    private static string[] Split(string? ids) =>
        string.IsNullOrWhiteSpace(ids) ? [] : ids.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
