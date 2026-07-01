using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Runs <see cref="GrammarFstAdvisor"/> against a real FLEx-exported grammar and prints the
/// report. [Explicit] so it never runs in CI. Point HC_GRAMMAR at an HC config XML:
///   $env:HC_GRAMMAR="...\sena-hc.xml"; dotnet test --filter "FullyQualifiedName~GrammarFstAdvisorBenchmark"
/// </summary>
[TestFixture]
[Explicit("Manual grammar-linter run against an external grammar; not part of CI.")]
public class GrammarFstAdvisorBenchmark
{
    [Test]
    public void Advise_OnExternalGrammar()
    {
        string? grammarPath = Environment.GetEnvironmentVariable("HC_GRAMMAR");
        Assert.That(grammarPath, Is.Not.Null.And.Not.Empty, "set HC_GRAMMAR to an HC config XML path");
        Assert.That(File.Exists(grammarPath), Is.True, $"grammar not found: {grammarPath}");

        Language language = XmlLanguageLoader.Load(grammarPath!);
        GrammarFstReport report = GrammarFstAdvisor.Analyze(language);

        TestContext.Out.WriteLine($"Grammar: {Path.GetFileName(grammarPath)}");
        TestContext.Out.WriteLine($"Strata : {language.Strata.Count}");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine(report.Format());
    }
}
