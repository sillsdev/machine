using System;
using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.FiniteState
{
	internal abstract class FstTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		private readonly IFstOperations<TData, TOffset> _operations; 

		protected FstTraversalMethod(IFstOperations<TData, TOffset> operations, Direction dir, Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults)
			: base(dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
			_operations = operations;
		}

		protected void ExecuteOutputs(IEnumerable<Output<TData, TOffset>> outputs, TData output, IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings,
			Queue<Annotation<TOffset>> queue)
		{
			Annotation<TOffset> prevNewAnn = null;
			foreach (Output<TData, TOffset> outputAction in outputs)
			{
				Annotation<TOffset> outputAnn;
				if (outputAction.UsePrevNewAnnotation && prevNewAnn != null)
				{
					outputAnn = prevNewAnn;
				}
				else
				{
					Annotation<TOffset> inputAnn = queue.Dequeue();
					outputAnn = mappings[inputAnn];
				}
				prevNewAnn = outputAction.UpdateOutput(output, outputAnn, _operations);
			}
		}
	}
}
