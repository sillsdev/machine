using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public TranslationResult Translate(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            return Translate(1, segment)[0];
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
        {
            CheckDisposed();

            return TranslateBatch(n, new[] { segment })[0];
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<IReadOnlyList<string>> segments)
        {
            CheckDisposed();

            return TranslateBatch(1, segments).Select(hypotheses => hypotheses[0]).ToArray();
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments
        )
        {
            CheckDisposed();

            var results = new List<IReadOnlyList<TranslationResult>>();
            foreach (var (inputTokens, inputLengths) in Batch(segments))
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
                        var builder = new TranslationResultBuilder();
                        for (long k = start; k < end; k++)
                            builder.AppendWord(outputTokenStrs[k], TranslationSources.Nmt);

                        NDArray alignment = alignments[i][j];
                        NDArray srcIndices = np.argmax(alignment[new Slice(stop: outputLength)], axis: -1);
                        int inputLength = inputLengths[i];
                        var waMatrix = new WordAlignmentMatrix(
                            inputLength,
                            outputLength,
                            srcIndices.Zip(Enumerable.Range(0, outputLength), (s, t) => ((int)s, t))
                        );
                        builder.MarkPhrase(Range.Create(0, inputLength), waMatrix);

                        hypotheses.Add(builder.ToResult(inputLength));
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

        private IEnumerable<(NDArray, NDArray)> Batch(IEnumerable<IReadOnlyList<string>> segments)
        {
            var batch = new List<IReadOnlyList<string>>();
            int maxLength = 0;
            foreach (IReadOnlyList<string> segment in segments)
            {
                maxLength = Math.Max(maxLength, segment.Count);
                batch.Add(segment);
                if (batch.Count == BatchSize)
                {
                    yield return (CreateArray(batch, maxLength), np.array(batch.Select(s => s.Count).ToArray()));
                    batch.Clear();
                    maxLength = 0;
                }
            }
            if (batch.Count > 0)
                yield return (CreateArray(batch, maxLength), np.array(batch.Select(s => s.Count).ToArray()));
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
