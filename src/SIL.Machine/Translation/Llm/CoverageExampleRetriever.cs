using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation.Llm
{
    public class CoverageExampleRetriever : ExampleRetriever
    {
        private const int Anchors = 20;
        private const int CandidateMultiple = 4;
        private const int MinCandidates = 100;
        private const double RelevanceWeight = 0.7;

        private readonly HashSet<int> _uniqueExampleIndices = new HashSet<int>();
        private readonly HashSet<int> _deduplicatedExampleIndices = new HashSet<int>();
        private TfidfIndex _index;

        public CoverageExampleRetriever(IEnumerable<Example> examples)
            : base(examples)
        {
            var seen = new Dictionary<Example, int>();
            for (int i = 0; i < ExampleList.Count; i++)
            {
                Example example = ExampleList[i];
                if (!seen.TryGetValue(example, out int firstIndex))
                {
                    _uniqueExampleIndices.Add(i);
                    _deduplicatedExampleIndices.Add(i);
                    seen[example] = i;
                }
                else
                {
                    _uniqueExampleIndices.Remove(firstIndex);
                }
            }
            Fit();
        }

        protected override void Fit()
        {
            _index = TfidfIndex.Create(ExampleList.Select(e => CharWbNgrams(e.Source)).ToArray());
        }

        protected override IReadOnlyList<Example> RankCore(string query, int k, int? excludeIndex)
        {
            if (_index == null)
                return Array.Empty<Example>();

            IReadOnlyDictionary<int, double> queryVector = _index.Vectorize(CharWbNgrams(query));
            double[] similarities = _index.Similarities(queryVector);
            // Highest similarity first; ties keep corpus order.
            List<int> ranked = Enumerable
                .Range(0, similarities.Length)
                .OrderByDescending(i => similarities[i])
                .ThenBy(i => i)
                .ToList();
            if (k >= ranked.Count)
                return ranked.Where(i => i != excludeIndex).Select(i => ExampleList[i]).ToList();

            // If excludeIndex points to the first occurrence of a multiply attested example,
            // that example is still included through its later copies.
            if (
                excludeIndex.HasValue
                && (
                    !_deduplicatedExampleIndices.Contains(excludeIndex.Value)
                    || _uniqueExampleIndices.Contains(excludeIndex.Value)
                )
            )
            {
                ranked.RemoveAll(i => i == excludeIndex.Value);
            }
            ranked = ranked.Where(_deduplicatedExampleIndices.Contains).ToList();
            k = Math.Min(k, ranked.Count);
            if (k == 0)
                return Array.Empty<Example>();
            if (queryVector.Count == 0)
                return ranked.Take(k).Select(i => ExampleList[i]).ToList();

            int limit = Math.Max(CandidateMultiple * k, MinCandidates);
            int[] candidates = ranked.Take(limit).ToArray();
            List<int> chosen = ChooseForCoverage(
                queryVector,
                candidates.Select(i => _index.Rows[i]).ToArray(),
                candidates.Select(i => similarities[i]).ToArray(),
                k
            );
            return chosen
                .Select(i => candidates[i])
                .OrderByDescending(i => similarities[i])
                .ThenBy(i => i)
                .Select(i => ExampleList[i])
                .ToList();
        }

        private static List<int> ChooseForCoverage(
            IReadOnlyDictionary<int, double> queryVector,
            IReadOnlyList<IReadOnlyDictionary<int, double>> candidateRows,
            double[] similarities,
            int k
        )
        {
            // Restrict the features that contribute to query coverage to those present in the query.
            int[] queryTerms = queryVector.Keys.ToArray();
            int candidateCount = candidateRows.Count;
            var features = new bool[candidateCount][];
            for (int c = 0; c < candidateCount; c++)
            {
                features[c] = new bool[queryTerms.Length];
                for (int f = 0; f < queryTerms.Length; f++)
                    features[c][f] = candidateRows[c].TryGetValue(queryTerms[f], out double v) && v > 0;
            }

            // Normalize the query weights and the candidate similarities.
            double weightSum = queryTerms.Sum(t => queryVector[t]);
            double[] weights = queryTerms.Select(t => queryVector[t] / weightSum).ToArray();
            double maxSimilarity = Math.Max(similarities.Max(), 1e-12);
            double[] relevance = similarities.Select(s => s / maxSimilarity).ToArray();

            // Always choose the first anchorCount most similar candidates.
            int anchorCount = Math.Min(Anchors, Math.Min(Math.Max(1, k / 2), candidateCount));
            var chosen = Enumerable.Range(0, anchorCount).ToList();
            var available = new bool[candidateCount];
            for (int c = 0; c < candidateCount; c++)
                available[c] = c >= anchorCount;

            // Track how many times each feature is represented across the chosen examples.
            var counts = new double[queryTerms.Length];
            foreach (int c in chosen)
                AddFeatures(counts, features[c]);

            // Greedily add the example with the highest composite relevance/coverage score.
            int target = Math.Min(k, candidateCount);
            var gains = new double[candidateCount];
            while (chosen.Count < target)
            {
                // The more important the n-gram is in the query, the higher the gain; the more times a chosen
                // example already has it, the lower the gain.
                var featureWeights = new double[queryTerms.Length];
                for (int f = 0; f < featureWeights.Length; f++)
                    featureWeights[f] = weights[f] / (1.0 + counts[f]);
                double maxGain = double.NegativeInfinity;
                for (int c = 0; c < candidateCount; c++)
                {
                    double gain = 0;
                    for (int f = 0; f < featureWeights.Length; f++)
                    {
                        if (features[c][f])
                            gain += featureWeights[f];
                    }
                    gains[c] = gain;
                    if (available[c] && gain > maxGain)
                        maxGain = gain;
                }
                // Normalize the gains so they are comparable to the normalized relevance.
                maxGain = Math.Max(maxGain, 1e-12);

                int best = -1;
                double bestUtility = double.NegativeInfinity;
                for (int c = 0; c < candidateCount; c++)
                {
                    if (!available[c])
                        continue;
                    double utility = RelevanceWeight * relevance[c] + (1 - RelevanceWeight) * (gains[c] / maxGain);
                    if (best < 0 || utility > bestUtility)
                    {
                        best = c;
                        bestUtility = utility;
                    }
                }
                chosen.Add(best);
                available[best] = false;
                AddFeatures(counts, features[best]);
            }
            return chosen;
        }

        private static void AddFeatures(double[] counts, bool[] features)
        {
            for (int f = 0; f < features.Length; f++)
            {
                if (features[f])
                    counts[f]++;
            }
        }

        /// <summary>Character 2- to 5-grams within word boundaries, as scikit-learn's char_wb does.</summary>
        private static IReadOnlyList<string> CharWbNgrams(string text)
        {
            var ngrams = new List<string>();
            foreach (string word in text.ToLowerInvariant().Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                string padded = " " + word + " ";
                for (int n = 2; n <= 5; n++)
                {
                    int offset = 0;
                    ngrams.Add(Slice(padded, offset, n));
                    while (offset + n < padded.Length)
                    {
                        offset++;
                        ngrams.Add(Slice(padded, offset, n));
                    }
                    // A word shorter than n is counted only once.
                    if (offset == 0)
                        break;
                }
            }
            return ngrams;
        }

        private static string Slice(string text, int start, int length) =>
            text.Substring(start, Math.Min(length, text.Length - start));
    }
}
