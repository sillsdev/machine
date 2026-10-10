using System.Reflection;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class CatalogBootstrapAuthorityTests
{
    [Test]
    public void BootstrapExposesProposalOutputButNoCanonicalCatalogWriter()
    {
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("fixture.dtd", "<!ELEMENT Root EMPTY>")
        );
        using var proposal = new StringWriter();

        CatalogBootstrap.WriteProposal(proposal, inventory);

        Assert.Multiple(() =>
        {
            Assert.That(proposal.ToString(), Is.EqualTo(CatalogBootstrap.Generate(inventory)));
            Assert.That(
                typeof(CatalogBootstrap)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Select(method => method.Name),
                Does.Not.Contain("Write"),
                "bootstrap code must have no API that can overwrite the canonical catalog"
            );
        });
    }

    [Test]
    public void ProposalScopesRoundTripAsExactSortedCanonicalIds()
    {
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd("fixture.dtd", "<!ELEMENT Root EMPTY>")
        );
        string[] scopes =
        {
            "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader.Load(System.String,System.Action`2<System.Exception,System.String>)",
            "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader.Load(System.Int32[])",
        };

        string proposal = CatalogBootstrap.Generate(inventory, scopes.Reverse().ToArray());
        SemanticCatalog parsed = SemanticCatalogLoader.Parse(proposal, "proposal.yaml");

        Assert.That(
            parsed.AuditedSourceScopes,
            Is.EqualTo(scopes.OrderBy(scope => scope, StringComparer.Ordinal).ToArray())
        );
    }
}
