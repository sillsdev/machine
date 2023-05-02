using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;
using Tensorflow;
using Tensorflow.NumPy;
using Range = SIL.Machine.Annotations.Range<int>;

namespace SIL.Machine.Translation.TensorFlow
{
    public class SavedModelTranslateSignature
    {
        public string SignatureKey { get; set; } = "serving_default";
        public string InputTokensKey { get; set; } = "tokens";
        public string InputLengthKey { get; set; } = "length";
        public string InputRefKey { get; set; } = "ref";
        public string InputRefLengthKey { get; set; } = "ref_length";
        public string OutputTokensKey { get; set; } = "tokens";
        public string OutputLengthKey { get; set; } = "length";
        public string OutputAlignmentKey { get; set; } = "alignment";
    }

    public class SavedModelNmtEngine : DisposableBase, ITranslationEngine
    {
        private readonly Session _session;
        private readonly object[] _outputs;
        private readonly Dictionary<string, int> _outputIndices;
        private readonly Dictionary<string, object> _inputs;
        private readonly SavedModelTranslateSignature _signature;

        public SavedModelNmtEngine(string modelFilename, SavedModelTranslateSignature signature = null)
        {
            _signature = signature ?? new SavedModelTranslateSignature();
            _session = Session.LoadFromSavedModel(modelFilename);
            byte[] bytes = File.ReadAllBytes(Path.Combine(modelFilename, "saved_model.pb"));
            var metadata = SavedModel.Parser.ParseFrom(bytes);
            bool found = false;
            foreach (MetaGraphDef metaGraph in metadata.MetaGraphs)
            {
                if (metaGraph.SignatureDef.TryGetValue(_signature.SignatureKey, out SignatureDef signatureDef))
                {
                    var outputs = new List<object>();
                    _outputIndices = new Dictionary<string, int>();
                    foreach (KeyValuePair<string, TensorInfo> kvp in signatureDef.Outputs)
                    {
                        _outputIndices[kvp.Key] = outputs.Count;
                        outputs.Add(GetOpTensor(kvp.Value));
                    }
                    _outputs = outputs.ToArray();

                    _inputs = signatureDef.Inputs.ToDictionary(kvp => kvp.Key, kvp => GetOpTensor(kvp.Value));

                    found = true;
                }
            }
            if (!found)
                throw new ArgumentException("The specified signature is not defined.", nameof(signature));
        }

        public int BatchSize { get; set; } = 32;
        public string PaddingToken { get; set; } = "<blank>";

        public ITokenizer<string, int, string> SourceTokenizer { get; set; } = WhitespaceTokenizer.Instance;
        public IDetokenizer<string, string> TargetDetokenizer { get; set; } = WhitespaceDetokenizer.Instance;

        public Task<TranslationResult> TranslateAsync(string segment, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Translate(segment));
        }

        public Task<TranslationResult> TranslateAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Translate(segment));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            string segment,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Translate(n, segment));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Translate(n, segment));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(segments));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(segments));
        }

        public Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(n, segments));
        }

        public Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(n, segments));
        }

        public TranslationResult Translate(string segment)
        {
            CheckDisposed();

            return Translate(1, segment)[0];
        }

        public TranslationResult Translate(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            return Translate(1, segment)[0];
        }

        public IReadOnlyList<TranslationResult> Translate(int n, string segment)
        {
            CheckDisposed();

            return TranslateBatch(n, new[] { segment })[0];
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
        {
            CheckDisposed();

            return TranslateBatch(n, new[] { segment })[0];
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<string> segments)
        {
            CheckDisposed();

            return TranslateBatch(1, segments).Select(hypotheses => hypotheses[0]).ToArray();
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<IReadOnlyList<string>> segments)
        {
            CheckDisposed();

            return TranslateBatch(1, segments).Select(hypotheses => hypotheses[0]).ToArray();
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(int n, IReadOnlyList<string> segments)
        {
            CheckDisposed();

            return TranslateBatch(n, segments.Select(s => SourceTokenizer.Tokenize(s).ToArray()).ToArray());
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments
        )
        {
            CheckDisposed();

            var results = new List<IReadOnlyList<TranslationResult>>();
            foreach (var (sourceTokenStrs, inputTokens, inputLengths) in Batch(segments))
            {
                var curBatchSize = (int)inputTokens.dims[0];
                NDArray refs = new NDArray(Enumerable.Repeat("", curBatchSize).ToArray(), new Shape(curBatchSize, 1));
                NDArray refsLengths = np.array(Enumerable.Repeat(1, curBatchSize).ToArray());

                _session.graph.as_default();
                NDArray[] sessionResults = _session.run(
                    _outputs,
                    (_inputs[_signature.InputTokensKey], inputTokens),
                    (_inputs[_signature.InputLengthKey], inputLengths),
                    (_inputs[_signature.InputRefKey], refs),
                    (_inputs[_signature.InputRefLengthKey], refsLengths)
                );

                NDArray outputTokens = sessionResults[_outputIndices[_signature.OutputTokensKey]];
                NDArray outputLengths = sessionResults[_outputIndices[_signature.OutputLengthKey]];
                NDArray alignments = sessionResults[_outputIndices[_signature.OutputAlignmentKey]];
                string[] outputTokenStrs = outputTokens.StringData();

                for (int i = 0; i < outputLengths.dims[0]; i++)
                {
                    var hypotheses = new List<TranslationResult>();
                    for (int j = 0; j < n && j < outputLengths.dims[1]; j++)
                    {
                        int outputLength = outputLengths[i][j];
                        long start = (i * outputTokens.dims[1] * outputTokens.dims[2]) + (j * outputTokens.dims[2]);
                        long end = start + outputLength;
                        var builder = new TranslationResultBuilder(sourceTokenStrs[i])
                        {
                            TargetDetokenizer = TargetDetokenizer
                        };
                        for (long k = start; k < end; k++)
                            builder.AppendToken(outputTokenStrs[k], TranslationSources.Nmt, 1);

                        NDArray alignment = alignments[i][j];
                        NDArray srcIndices = np.argmax(alignment[new Slice(stop: outputLength)], axis: -1);
                        int inputLength = inputLengths[i];
                        var waMatrix = new WordAlignmentMatrix(
                            inputLength,
                            outputLength,
                            srcIndices.Zip(Enumerable.Range(0, outputLength), (s, t) => ((int)s, t))
                        );
                        builder.MarkPhrase(Range.Create(0, inputLength), waMatrix);

                        hypotheses.Add(builder.ToResult());
                    }
                    results.Add(hypotheses);
                }
            }
            return results;
        }

        protected override void DisposeManagedResources()
        {
            _session.Dispose();
        }

        private object GetOpTensor(TensorInfo ti)
        {
            string[] parts = ti.Name.Split(new[] { ':' }, count: 2);
            Operation op = _session.graph.as_default().OperationByName(parts[0]);
            return op.outputs[int.Parse(parts[1])];
        }

        private IEnumerable<(IReadOnlyList<IReadOnlyList<string>>, NDArray, NDArray)> Batch(
            IEnumerable<IReadOnlyList<string>> segments
        )
        {
            var batch = new List<IReadOnlyList<string>>();
            int maxLength = 0;
            foreach (IReadOnlyList<string> tokens in segments)
            {
                maxLength = Math.Max(maxLength, tokens.Count);
                batch.Add(tokens);
                if (batch.Count == BatchSize)
                {
                    yield return (batch, CreateArray(batch, maxLength), np.array(batch.Select(s => s.Count).ToArray()));
                    batch = new List<IReadOnlyList<string>>();
                    maxLength = 0;
                }
            }
            if (batch.Count > 0)
                yield return (batch, CreateArray(batch, maxLength), np.array(batch.Select(s => s.Count).ToArray()));
        }

        private NDArray CreateArray(List<IReadOnlyList<string>> batch, int maxLength)
        {
            var tokens = new List<string>();
            foreach (IReadOnlyList<string> segment in batch)
            {
                tokens.AddRange(segment);
                tokens.AddRange(Enumerable.Repeat(PaddingToken, maxLength - segment.Count));
            }
            return new NDArray(tokens.ToArray(), new Shape(batch.Count, maxLength));
        }
    }
}
