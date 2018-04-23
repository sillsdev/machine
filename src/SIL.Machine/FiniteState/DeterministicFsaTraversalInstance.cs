using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
	internal class DeterministicFsaTraversalInstance<TData, TOffset> : TraversalInstance<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		public DeterministicFsaTraversalInstance(int registerCount)
			: base(registerCount, true)
		{
		}
	}
}
