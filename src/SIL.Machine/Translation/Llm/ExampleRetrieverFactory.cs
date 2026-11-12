using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation.Llm
{
    public static class ExampleRetrieverFactory
    {
        public static ExampleRetriever CreateExampleRetriever(
            ExampleRetrievalMethod method,
            IEnumerable<Example> examples
        )
        {
            switch (method)
            {
                case ExampleRetrievalMethod.Tfidf:
                    return new TfidfExampleRetriever(examples);
                case ExampleRetrievalMethod.Coverage:
                    return new CoverageExampleRetriever(examples);
                default:
                    throw new ArgumentException(
                        $"Unable to create example retriever with method {method}.",
                        nameof(method)
                    );
            }
        }
    }
}
