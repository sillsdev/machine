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
	public class TensorFlowTranslateSignature
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

	public class TensorFlowNmtEngine : DisposableBase, ITranslationEngine
	{
		private readonly Session _session;
		private readonly object[] _outputs;
		private readonly Dictionary<string, int> _outputIndices;
		private readonly Dictionary<string, object> _inputs;
		private readonly TensorFlowTranslateSignature _signature;

		public TensorFlowNmtEngine(string modelFilename, TensorFlowTranslateSignature signature = null)
		{
			_signature = signature ?? new TensorFlowTranslateSignature();
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

		public TranslationResult Translate(IReadOnlyList<string> segment)
		{
			return Translate(1, segment).First();
		}

		public IEnumerable<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
		{
			NDArray inputTokens = new NDArray(segment.ToArray(), new Shape(1, segment.Count));
			NDArray inputLength = np.array(new[] { segment.Count });
			NDArray refs = new NDArray(new[] { "" }, new Shape(1, 1));
			NDArray refsLength = np.array(new[] { 1 });

			_session.graph.as_default();
			NDArray[] results = _session.run(_outputs,
				(_inputs[_signature.InputTokensKey], inputTokens),
				(_inputs[_signature.InputLengthKey], inputLength),
				(_inputs[_signature.InputRefKey], refs),
				(_inputs[_signature.InputRefLengthKey], refsLength));

			NDArray outputTokens = results[_outputIndices[_signature.OutputTokensKey]];
			NDArray outputLengths = results[_outputIndices[_signature.OutputLengthKey]];
			NDArray alignments = results[_outputIndices[_signature.OutputAlignmentKey]];
			string[] outputTokenStrs = outputTokens.StringData();
			for (int i = 0; i < n || i < outputLengths.dims[0]; i++)
			{
				int outputLength = outputLengths[0][i];
				long start = outputTokens.dims[2] * i;
				long end = start + outputLength;
				var builder = new TranslationResultBuilder();
				for (long j = start; j < end; j++)
					builder.AppendWord(outputTokenStrs[j], TranslationSources.Nmt);

				NDArray alignment = alignments[0][i];
				NDArray srcIndices = np.argmax(alignment[new Slice(stop: outputLength)], axis: -1);
				var waMatrix = new WordAlignmentMatrix(segment.Count, outputLength,
					srcIndices.Zip(Enumerable.Range(0, outputLength), (s, t) => ((int)s, t)));
				builder.MarkPhrase(Range.Create(0, segment.Count), waMatrix);

				yield return builder.ToResult(segment);
			}
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
	}
}
