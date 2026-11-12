using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation.Llm
{
    /// <summary>
    /// A sparse, L2-normalized TF-IDF index over pre-tokenized documents. It follows scikit-learn's
    /// <c>TfidfVectorizer</c> defaults: raw term counts, smoothed idf, unit-length rows.
    /// </summary>
    internal class TfidfIndex
    {
        private readonly Dictionary<string, int> _vocabulary = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<double> _idf = new List<double>();

        /// <summary>Returns null when no document has any term.</summary>
        public static TfidfIndex Create(IReadOnlyList<IReadOnlyList<string>> documents)
        {
            var index = new TfidfIndex(documents);
            return index._vocabulary.Count == 0 ? null : index;
        }

        private TfidfIndex(IReadOnlyList<IReadOnlyList<string>> documents)
        {
            var documentFrequency = new List<int>();
            foreach (IReadOnlyList<string> document in documents)
            {
                foreach (string term in new HashSet<string>(document, StringComparer.Ordinal))
                {
                    if (!_vocabulary.TryGetValue(term, out int termIndex))
                    {
                        termIndex = _vocabulary.Count;
                        _vocabulary[term] = termIndex;
                        documentFrequency.Add(0);
                    }
                    documentFrequency[termIndex]++;
                }
            }
            int n = documents.Count;
            foreach (int df in documentFrequency)
                _idf.Add(Math.Log((1.0 + n) / (1.0 + df)) + 1.0);

            Rows = documents.Select(Vectorize).ToArray();
        }

        /// <summary>The normalized vector of each indexed document, keyed by term index.</summary>
        public IReadOnlyList<IReadOnlyDictionary<int, double>> Rows { get; }

        /// <summary>Vectorizes a query. Terms outside the indexed vocabulary are dropped.</summary>
        public IReadOnlyDictionary<int, double> Vectorize(IReadOnlyList<string> terms)
        {
            var vector = new Dictionary<int, double>();
            foreach (string term in terms)
            {
                if (_vocabulary.TryGetValue(term, out int termIndex))
                {
                    vector.TryGetValue(termIndex, out double count);
                    vector[termIndex] = count + 1;
                }
            }
            foreach (int termIndex in vector.Keys.ToList())
                vector[termIndex] *= _idf[termIndex];
            double norm = Math.Sqrt(vector.Values.Sum(v => v * v));
            if (norm > 0)
            {
                foreach (int termIndex in vector.Keys.ToList())
                    vector[termIndex] /= norm;
            }
            return vector;
        }

        /// <summary>Cosine similarity of the query vector to every indexed document.</summary>
        public double[] Similarities(IReadOnlyDictionary<int, double> query)
        {
            var similarities = new double[Rows.Count];
            if (query.Count == 0)
                return similarities;
            for (int i = 0; i < Rows.Count; i++)
            {
                double sum = 0;
                foreach (KeyValuePair<int, double> entry in query)
                {
                    if (Rows[i].TryGetValue(entry.Key, out double value))
                        sum += entry.Value * value;
                }
                similarities[i] = sum;
            }
            return similarities;
        }
    }
}
