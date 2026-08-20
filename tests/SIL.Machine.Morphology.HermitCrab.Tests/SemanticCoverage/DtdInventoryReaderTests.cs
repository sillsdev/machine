using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class DtdInventoryReaderTests
{
    [Test]
    public void TinyDtdProducesCompleteDeterministicManifest()
    {
        const string Dtd = """
            <!-- leading comment -->
            <!ELEMENT Root ( Child? )>
            <!ATTLIST Root
                mode (one | two) "one"
                target IDREF #IMPLIED
            >
            <!ELEMENT Child (#PCDATA)>
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("fixture.dtd", Dtd)
        );

        Assert.That(
            inventory.Surfaces.Select(surface => surface.Id),
            Is.EqualTo(
                new[]
                {
                    "dtd:attribute-default/Root/mode/default/one",
                    "dtd:attribute-default/Root/target/implied",
                    "dtd:attribute-type/Root/mode/enumeration",
                    "dtd:attribute-type/Root/target/IDREF",
                    "dtd:attribute/Root/mode",
                    "dtd:attribute/Root/target",
                    "dtd:content/Child/r.pcdata@one",
                    "dtd:content/Root/r.sequence@one",
                    "dtd:default/Root/mode/one",
                    "dtd:element/Child",
                    "dtd:element/Root",
                    "dtd:enum/Root/mode/one",
                    "dtd:enum/Root/mode/two",
                    "dtd:placement/Root/r.0/Child/optional",
                }
            )
        );
        Assert.That(
            inventory.Surfaces.Single(surface => surface.Id == "dtd:attribute/Root/target").Value,
            Does.Contain("type=IDREF").And.Contain("default=#IMPLIED")
        );
        Assert.That(
            inventory.Surfaces.Single(surface => surface.Id == "dtd:placement/Root/r.0/Child/optional").Value,
            Does.Contain("group=dtd:content/Root/r.sequence@one")
        );
        Assert.That(inventory.SourceHash, Does.Match("^[0-9a-f]{64}$"));
    }

    [Test]
    public void NestedAlternationAndRepeatedPlacementsPreserveGroupTopology()
    {
        const string Dtd = """
            <!ELEMENT Root (A, (B | C)+, B?)>
            <!ELEMENT A EMPTY>
            <!ELEMENT B (#PCDATA)>
            <!ELEMENT C (#PCDATA)>
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("nested.dtd", Dtd)
        );

        Assert.That(
            inventory.Surfaces.Select(surface => surface.Id),
            Is.EqualTo(
                new[]
                {
                    "dtd:content/A/r.empty@one",
                    "dtd:content/B/r.pcdata@one",
                    "dtd:content/C/r.pcdata@one",
                    "dtd:content/Root/r.1.choice@one-or-more",
                    "dtd:content/Root/r.sequence@one",
                    "dtd:element/A",
                    "dtd:element/B",
                    "dtd:element/C",
                    "dtd:element/Root",
                    "dtd:placement/Root/r.0/A/one",
                    "dtd:placement/Root/r.1.0/B/one",
                    "dtd:placement/Root/r.1.1/C/one",
                    "dtd:placement/Root/r.2/B/optional",
                }
            )
        );
        Assert.That(
            inventory.Surfaces.Single(surface => surface.Id == "dtd:placement/Root/r.1.0/B/one").Value,
            Does.Contain("group=dtd:content/Root/r.1.choice@one-or-more").And.Contain("groupMax=unbounded")
        );
        Assert.That(
            inventory.Surfaces.Single(surface => surface.Id == "dtd:placement/Root/r.2/B/optional").Value,
            Does.Contain("group=dtd:content/Root/r.sequence@one")
        );
    }

    [Test]
    public void CommentsWhitespaceFixedAndMultilineAttributesAreParsed()
    {
        const string Dtd = """
            <!-- declaration comments -->
            <!ELEMENT Root (Item*)>
            <!ELEMENT Item (#PCDATA)>
            <!ATTLIST Root
                id ID #REQUIRED
                status
                  (ready
                   | done)
                  #FIXED "ready"
            >
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("comments.dtd", Dtd)
        );

        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:attribute/Root/id"));
        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:enum/Root/status/ready"));
        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:enum/Root/status/done"));
        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:default/Root/status/ready"));
        Assert.That(
            inventory.Surfaces.Single(surface => surface.Id == "dtd:attribute/Root/status").Value,
            Does.Contain("fixed=true").And.Contain("type=enumeration")
        );
    }

    [Test]
    public void StructuralPathsDistinguishEquivalentOrdinalShapes()
    {
        const string LeftNested = """
            <!ELEMENT Root (A, (B, C))>
            <!ELEMENT A EMPTY>
            <!ELEMENT B EMPTY>
            <!ELEMENT C EMPTY>
            """;
        const string RightNested = """
            <!ELEMENT Root ((A, B), C)>
            <!ELEMENT A EMPTY>
            <!ELEMENT B EMPTY>
            <!ELEMENT C EMPTY>
            """;

        SemanticInventory left = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("left.dtd", LeftNested)
        );
        SemanticInventory right = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("right.dtd", RightNested)
        );

        Assert.That(
            left.Surfaces.Select(surface => surface.Id),
            Is.Not.EqualTo(right.Surfaces.Select(surface => surface.Id))
        );
        Assert.That(left.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:content/Root/r.1.sequence@one"));
        Assert.That(right.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:content/Root/r.0.sequence@one"));
        Assert.That(left.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:placement/Root/r.1.0/B/one"));
        Assert.That(right.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:placement/Root/r.0.1/B/one"));
    }

    [Test]
    public void GroupCardinalityIsPartOfTheAddressableIdentity()
    {
        const string OneOrMore = """
            <!ELEMENT Root (A, B)+>
            <!ELEMENT A EMPTY>
            <!ELEMENT B EMPTY>
            """;
        const string ZeroOrMore = """
            <!ELEMENT Root (A, B)*>
            <!ELEMENT A EMPTY>
            <!ELEMENT B EMPTY>
            """;

        SemanticInventory oneOrMore = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("one-or-more.dtd", OneOrMore)
        );
        SemanticInventory zeroOrMore = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("zero-or-more.dtd", ZeroOrMore)
        );

        Assert.That(
            oneOrMore.Surfaces.Select(surface => surface.Id),
            Does.Contain("dtd:content/Root/r.sequence@one-or-more")
        );
        Assert.That(
            zeroOrMore.Surfaces.Select(surface => surface.Id),
            Does.Contain("dtd:content/Root/r.sequence@zero-or-more")
        );
        Assert.That(
            oneOrMore.Surfaces.Select(surface => surface.Id),
            Is.Not.EqualTo(zeroOrMore.Surfaces.Select(surface => surface.Id))
        );
    }

    [Test]
    public void AttributeTypePresenceAndAuthoredValuesHaveExactEncodedSurfaces()
    {
        const string Dtd = """
            <!ELEMENT Root EMPTY>
            <!ATTLIST Root
                required IDREF #REQUIRED
                implied IDREFS #IMPLIED
                ordinary CDATA "a/b; c"
                fixed CDATA #FIXED "a/b; c"
                quotedRequired CDATA "#REQUIRED"
                quotedImplied CDATA "#IMPLIED"
                fixedRequired CDATA #FIXED "#REQUIRED"
                fixedImplied CDATA #FIXED "#IMPLIED"
                colon:name IDREFS #FIXED "x:y"
            >
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("attributes.dtd", Dtd)
        );
        var ids = inventory.Surfaces.Select(surface => surface.Id).ToArray();

        Assert.That(ids, Does.Contain("dtd:attribute-type/Root/required/IDREF"));
        Assert.That(ids, Does.Contain("dtd:attribute-type/Root/implied/IDREFS"));
        Assert.That(ids, Does.Contain("dtd:attribute-type/Root/ordinary/CDATA"));
        Assert.That(ids, Does.Contain("dtd:attribute-type/Root/fixed/CDATA"));
        Assert.That(ids, Does.Contain("dtd:attribute-type/Root/colon%3Aname/IDREFS"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/required/required"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/implied/implied"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/ordinary/default/a%2Fb%3B%20c"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/fixed/fixed/a%2Fb%3B%20c"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/quotedRequired/default/%23REQUIRED"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/quotedImplied/default/%23IMPLIED"));
        Assert.That(ids, Does.Contain("dtd:default/Root/quotedRequired/%23REQUIRED"));
        Assert.That(ids, Does.Contain("dtd:default/Root/quotedImplied/%23IMPLIED"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/fixedRequired/fixed/%23REQUIRED"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/fixedImplied/fixed/%23IMPLIED"));
        Assert.That(ids, Does.Contain("dtd:default/Root/fixedRequired/%23REQUIRED"));
        Assert.That(ids, Does.Contain("dtd:default/Root/fixedImplied/%23IMPLIED"));
        Assert.That(ids, Does.Contain("dtd:attribute-default/Root/colon%3Aname/fixed/x%3Ay"));
        Assert.That(ids, Does.Contain("dtd:default/Root/ordinary/a%2Fb%3B%20c"));
        Assert.That(ids, Does.Contain("dtd:default/Root/fixed/a%2Fb%3B%20c"));
        Assert.That(ids, Does.Contain("dtd:default/Root/colon%3Aname/x%3Ay"));
        Assert.That(ids, Does.Contain("dtd:attribute/Root/colon%3Aname"));
    }

    [TestCase("<!ELEMENTX Root EMPTY>", "ELEMENT")]
    [TestCase("<!ATTLISTX Root id ID #IMPLIED>", "ATTLIST")]
    [TestCase("<!DOCTYPEX Root>", "DOCTYPE")]
    public void DeclarationKeywordsRequireXmlWhitespace(string dtd, string keyword)
    {
        string path = $"boundary-{keyword}.dtd";
        var error = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd(path, dtd))
        );

        Assert.That(error!.Message, Does.Contain("unsupported DTD declaration").And.Contain(path + ":1:"));
    }

    [Test]
    public void DeclarationSeparatorsUseOnlyXmlDtdWhitespace()
    {
        const string Dtd = "<!ELEMENT\fRoot EMPTY>";

        var error = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("separator.dtd", Dtd))
        );

        Assert.That(error!.Message, Does.Contain("unsupported DTD declaration").And.Contain("separator.dtd:1:"));
    }

    [TestCase("<!DOCTYPE Root>")]
    [TestCase("<!DOCTYPE Root SYSTEM \"external.dtd\">")]
    [TestCase("<!DOCTYPE Root PUBLIC \"public-id\" \"external.dtd\">")]
    [TestCase("<!DOCTYPE Root [ <!ELEMENT Root EMPTY> ]>")]
    public void EveryDoctypeDeclarationIsRejectedAtTheDtdSeam(string dtd)
    {
        var error = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("doctype.dtd", dtd))
        );

        Assert.That(
            error!.Message,
            Does.Contain("DOCTYPE declarations are unsupported").And.Contain("not loaded").And.Contain("doctype.dtd:1:")
        );
    }

    [TestCase("(#PCDATA | Child)")]
    [TestCase("(#PCDATA, Child)")]
    [TestCase("(Child | #PCDATA)*")]
    [TestCase("(#PCDATA)*")]
    [TestCase("((#PCDATA | Child)*)")]
    [TestCase("(#PCDATA | Child+)*")]
    [TestCase("(#PCDATA | Child | Child)*")]
    public void InvalidMixedContentShapesAreRejected(string model)
    {
        string dtd = $"""
            <!ELEMENT Root {model}>
            <!ELEMENT Child EMPTY>
            """;

        var error = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("mixed.dtd", dtd))
        );

        Assert.That(error!.Message, Does.Contain("mixed content model").And.Contain("mixed.dtd:1:"));
    }

    [Test]
    public void ValidMixedContentShapeIsEnumerated()
    {
        const string Dtd = """
            <!ELEMENT Root (#PCDATA | Child | Other)*>
            <!ELEMENT Child EMPTY>
            <!ELEMENT Other EMPTY>
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("valid-mixed.dtd", Dtd)
        );
        var ids = inventory.Surfaces.Select(surface => surface.Id);

        Assert.That(ids, Does.Contain("dtd:content/Root/r.choice@zero-or-more"));
        Assert.That(ids, Does.Contain("dtd:content/Root/r.0.pcdata@one"));
        Assert.That(ids, Does.Contain("dtd:placement/Root/r.1/Child/one"));
        Assert.That(ids, Does.Contain("dtd:placement/Root/r.2/Other/one"));
    }

    [TestCase("\"three\"")]
    [TestCase("#FIXED \"three\"")]
    public void EnumerationDefaultsMustBeExactMembers(string defaultDeclaration)
    {
        string dtd = $"""
            <!ELEMENT Root EMPTY>
            <!ATTLIST Root mode (one | two) {defaultDeclaration}>
            """;

        var error = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("enum-default.dtd", dtd))
        );

        Assert.That(error!.Message, Does.Contain("not an enumeration member").And.Contain("enum-default.dtd:2:"));
    }

    [TestCase("<!ELEMENT Root(Child)>")]
    [TestCase("<!ATTLIST Root id ID#IMPLIED>")]
    [TestCase("<!ATTLIST Root mode (one|two)#IMPLIED>")]
    [TestCase("<!ATTLIST Root mode CDATA #FIXED\"x\">")]
    [TestCase("<!ATTLIST Root mode CDATA \"x\"b CDATA \"y\">")]
    [TestCase("<!ELEMENT Root (Child ?)>")]
    [TestCase("<!ELEMENT Root (Child) ?>")]
    public void MissingDtdTokenSeparatorsAreRejected(string dtd)
    {
        Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("separator-boundary.dtd", dtd))
        );
    }

    [TestCase("<!ATTLIST Root id ID#IMPLIED>")]
    [TestCase("<!ATTLIST Root mode (one|two)#IMPLIED>")]
    [TestCase("<!ATTLIST Root mode CDATA #FIXED\"x\">")]
    [TestCase("<!ATTLIST Root mode CDATA \"x\"b CDATA \"y\">")]
    public void MissingAttributeSeparatorsHaveExplicitDiagnostics(string dtd)
    {
        var error = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("attribute-separator.dtd", dtd))
        );

        Assert.That(
            error!.Message,
            Does.Contain("expected XML DTD whitespace").And.Contain("attribute-separator.dtd:1:")
        );
    }

    [TestCase("<!-- bad -- <!ELEMENT Hidden EMPTY> --><!ELEMENT Root EMPTY>")]
    [TestCase("<!-- bad -- comment --><!ELEMENT Root EMPTY>")]
    [TestCase("<!-- bad ---><!ELEMENT Root EMPTY>")]
    public void MalformedCommentsAreRejectedBeforeTheyCanHideDeclarations(string dtd)
    {
        var error = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("comment.dtd", dtd))
        );

        Assert.That(error!.Message, Does.Contain("invalid DTD comment").And.Contain("comment.dtd:1:"));
    }

    [Test]
    public void ValidCommentsDoNotHideFollowingDeclarations()
    {
        const string Dtd = """
            <!-- valid declaration comment -->
            <!ELEMENT Root EMPTY>
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("valid-comment.dtd", Dtd)
        );

        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:element/Root"));
        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:content/Root/r.empty@one"));
    }

    [Test]
    public void UnsupportedNotationAndInvalidPublicInputsFailDeterministically()
    {
        const string NotationDtd = """
            <!ELEMENT Root EMPTY>
            <!ATTLIST Root notation NOTATION (one | two) #IMPLIED>
            """;

        var notationError = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("notation.dtd", NotationDtd))
        );
        Assert.That(notationError!.Message, Does.Contain("unsupported attribute type").And.Contain("notation.dtd:2:"));

        Assert.Throws<ArgumentException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("", "<!ELEMENT Root EMPTY>"))
        );
        Assert.Throws<ArgumentNullException>(() => SemanticCoverageInventory.Generate(null!));
    }

    [Test]
    public void DuplicatesAndMalformedDeclarationsFailWithSourceSpan()
    {
        const string Duplicate = """
            <!ELEMENT Root EMPTY>
            <!ELEMENT Root ANY>
            """;
        const string Unterminated = "<!ELEMENT Root (Child?)";

        var duplicateError = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("duplicate.dtd", Duplicate))
        );
        Assert.That(duplicateError!.Message, Does.Contain("duplicate").And.Contain("duplicate.dtd:2:"));

        var malformedError = Assert.Throws<SemanticCoverageParseException>(() =>
            SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("broken.dtd", Unterminated))
        );
        Assert.That(malformedError!.Message, Does.Contain("unterminated").And.Contain("broken.dtd:1:"));
    }

    [Test]
    public void AuthoritativeHermitCrabDtdCanBeEnumerated()
    {
        string dtdPath = Path.GetFullPath(
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "SIL.Machine.Morphology.HermitCrab",
                "HermitCrabInput.dtd"
            )
        );
        string dtd = File.ReadAllText(dtdPath);
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("HermitCrabInput.dtd", dtd)
        );

        var kindCounts = inventory
            .Surfaces.GroupBy(surface => surface.Kind)
            .ToDictionary(group => group.Key, group => group.Count());
        Assert.That(inventory.Surfaces.Count, Is.EqualTo(1059));
        Assert.That(kindCounts["element"], Is.EqualTo(108));
        Assert.That(kindCounts["attribute"], Is.EqualTo(149));
        Assert.That(kindCounts["attribute-type"], Is.EqualTo(149));
        Assert.That(kindCounts["attribute-default"], Is.EqualTo(149));
        Assert.That(kindCounts["enum"], Is.EqualTo(156));
        Assert.That(kindCounts["default"], Is.EqualTo(54));
        Assert.That(kindCounts["content-group"], Is.EqualTo(91));
        Assert.That(kindCounts["special-content"], Is.EqualTo(19));
        Assert.That(kindCounts["placement"], Is.EqualTo(184));
        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:element/HermitCrabInput"));
        Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:attribute/Language/isActive"));
    }

    [Test]
    public void CarrierCardinalityAndEnumMutationsChangeGeneratedDenominator()
    {
        const string BaseDtd = """
            <!ELEMENT Root (Child)>
            <!ELEMENT Child EMPTY>
            <!ATTLIST Root mode (one | two) "one">
            """;
        const string CarrierMutation = """
            <!ELEMENT Root (Child, Carrier)>
            <!ELEMENT Child EMPTY>
            <!ELEMENT Carrier EMPTY>
            <!ATTLIST Root mode (one | two) "one">
            """;
        const string CardinalityMutation = """
            <!ELEMENT Root (Child+)>
            <!ELEMENT Child EMPTY>
            <!ATTLIST Root mode (one | two) "one">
            """;
        const string EnumMutation = """
            <!ELEMENT Root (Child)>
            <!ELEMENT Child EMPTY>
            <!ATTLIST Root mode (one | two | three) "one">
            """;

        var baseline = SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("base.dtd", BaseDtd));
        var carrier = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("carrier.dtd", CarrierMutation)
        );
        var cardinality = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("cardinality.dtd", CardinalityMutation)
        );
        var @enum = SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("enum.dtd", EnumMutation));

        Assert.That(
            carrier.Surfaces.Select(surface => surface.Id),
            Is.Not.EqualTo(baseline.Surfaces.Select(surface => surface.Id))
        );
        Assert.That(
            cardinality.Surfaces.Select(surface => surface.Id),
            Is.Not.EqualTo(baseline.Surfaces.Select(surface => surface.Id))
        );
        Assert.That(
            @enum.Surfaces.Select(surface => surface.Id),
            Is.Not.EqualTo(baseline.Surfaces.Select(surface => surface.Id))
        );
        Assert.That(@enum.Surfaces.Select(surface => surface.Id), Does.Contain("dtd:enum/Root/mode/three"));
        Assert.That(
            cardinality.Surfaces.Select(surface => surface.Id),
            Does.Contain("dtd:placement/Root/r.0/Child/one-or-more")
        );
        Assert.That(
            cardinality.Surfaces.Single(surface => surface.Id == "dtd:placement/Root/r.0/Child/one-or-more").Value,
            Does.Contain("min=1").And.Contain("max=unbounded")
        );
    }
}
