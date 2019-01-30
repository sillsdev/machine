using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	internal class WordEditDistance : EditDistance<string, char>
	{
		public double HitCost { get; set; }
		public double InsertionCost { get; set; }
		public double DeletionCost { get; set; }
		public double SubstitutionCost { get; set; }

		public double Compute(string x, string y, out IEnumerable<EditOperation> ops)
		{
			double[,] distMatrix;
			double dist = ComputeDistMatrix(x, y, true, false, out distMatrix);
			ops = GetOperations(x, y, distMatrix, true, false, GetCount(x), GetCount(y));
			return dist;
		}

		public double ComputePrefix(string x, string y, bool isLastItemComplete, bool usePrefixDelOp,
			out IEnumerable<EditOperation> ops)
		{
			double[,] distMatrix;
			double dist = ComputeDistMatrix(x, y, isLastItemComplete, usePrefixDelOp, out distMatrix);
			ops = GetOperations(x, y, distMatrix, isLastItemComplete, usePrefixDelOp, GetCount(x), GetCount(y));
			return dist;
		}

		protected override int GetCount(string item)
		{
			return item.Length;
		}

		protected override char GetItem(string seq, int index)
		{
			return seq[index];
		}

		protected override double GetHitCost(char x, char y, bool isComplete)
		{
			return HitCost;
		}

		protected override double GetSubstitutionCost(char x, char y, bool isComplete)
		{
			return SubstitutionCost;
		}

		protected override double GetDeletionCost(char x)
		{
			return DeletionCost;
		}

		protected override double GetInsertionCost(char y)
		{
			return InsertionCost;
		}

		protected override bool IsHit(char x, char y, bool isComplete)
		{
			return x == y;
		}
	}
}
