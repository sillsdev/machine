namespace SIL.Machine.Translation.Llm;

public class MockExampleRetriever : TopKExampleRetriever
{
    public MockExampleRetriever(IEnumerable<Example> examples)
        : base(examples)
    {
        Fit();
    }

    protected override void Fit() { }

    protected override double[] ScoreExamples(string query)
    {
        return ExampleList
            .Select(e =>
            {
                int comparison = string.CompareOrdinal(query, e.Source);
                return comparison == 0 ? 0.0
                    : comparison > 0 ? 1.0
                    : -1.0;
            })
            .ToArray();
    }
}
