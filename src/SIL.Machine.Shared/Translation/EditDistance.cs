using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public enum EditOperation
	{
		None,
		Hit,
		Insert,
		Delete,
		PrefixDelete,
		Substitute
	}

	public abstract class EditDistance<TSeq, TItem>
	{
		public virtual double HitCost { get; set; }
		public virtual double InsertionCost { get; set; }
		public virtual double SubstitutionCost { get; set; }
		public virtual double DeletionCost { get; set; }

		public virtual double Compute(TSeq x, TSeq y)
		{
			return Compute(x, y, true, false);
		}

		public virtual double Compute(TSeq x, TSeq y, out IEnumerable<EditOperation> ops)
		{
			return Compute(x, y, true, false, out ops);
		}

		public virtual double ComputePrefix(TSeq x, TSeq y, bool isLastItemComplete, bool usePrefixDelOp,
			out IEnumerable<EditOperation> ops)
		{
			return Compute(x, y, isLastItemComplete, usePrefixDelOp, out ops);
		}

		protected abstract int GetCount(TSeq item);
		protected abstract TItem GetItem(TSeq seq, int index);
		protected abstract double GetHitCost(TItem x, TItem y, bool isComplete);
		protected abstract double GetSubstitutionCost(TItem x, TItem y, bool isComplete);
		protected abstract double GetDeletionCost(TItem x);
		protected abstract double GetInsertionCost(TItem y);
		protected abstract bool IsHit(TItem x, TItem y, bool isComplete);

		protected double[,] InitDistMatrix(TSeq x, TSeq y)
		{
			int xCount = GetCount(x);
			int yCount = GetCount(y);
			int dim = Math.Max(xCount, yCount);
			var distMatrix = new double[dim + 1, dim + 1];
			return distMatrix;
		}

		private IEnumerable<EditOperation> GetOperations(TSeq x, TSeq y, double[,] distMatrix, bool isLastItemComplete,
			bool usePrefixDelOp, int i, int j)
		{
			int yCount = GetCount(y);
			var ops = new Stack<EditOperation>();
			while (i > 0 || j > 0)
			{
				EditOperation op;
				ProcessMatrixCell(x, y, distMatrix, usePrefixDelOp, j != yCount || isLastItemComplete, i, j, out i,
					out j, out op);
				if (op != EditOperation.PrefixDelete)
					ops.Push(op);
			}
			return ops;
		}

		private double Compute(TSeq x, TSeq y, bool isLastItemComplete, bool usePrefixDelOp)
		{
			double[,] distMatrix;
			return Compute(x, y, isLastItemComplete, usePrefixDelOp, out distMatrix);
		}

		private double Compute(TSeq x, TSeq y, bool isLastItemComplete, bool usePrefixDelOp,
			out IEnumerable<EditOperation> ops)
		{
			double[,] distMatrix;
			double dist = Compute(x, y, isLastItemComplete, usePrefixDelOp, out distMatrix);
			ops = GetOperations(x, y, distMatrix, isLastItemComplete, usePrefixDelOp, GetCount(x), GetCount(y));
			return dist;
		}

		protected double Compute(TSeq x, TSeq y, bool isLastItemComplete, bool usePrefixDelOp, out double[,] distMatrix)
		{
			distMatrix = InitDistMatrix(x, y);

			int xCount = GetCount(x);
			int yCount = GetCount(y);
			for (int i = 0; i <= xCount; i++)
			{
				for (int j = 0; j <= yCount; j++)
				{
					int iPred, jPred;
					EditOperation op;
					distMatrix[i, j] = ProcessMatrixCell(x, y, distMatrix, usePrefixDelOp,
						j != yCount || isLastItemComplete, i, j, out iPred, out jPred, out op);
				}
			}

			return distMatrix[xCount, yCount];
		}

		protected double ProcessMatrixCell(TSeq x, TSeq y, double[,] distMatrix, bool usePrefixDelOp, bool isComplete,
			int i, int j, out int iPred, out int jPred, out EditOperation op)
		{
			if (i != 0 && j != 0)
			{
				TItem xItem = GetItem(x, i - 1);
				TItem yItem = GetItem(y, j - 1);
				double substCost;
				if (IsHit(xItem, yItem, isComplete))
				{
					substCost = GetHitCost(xItem, yItem, isComplete);
					op = EditOperation.Hit;
				}
				else
				{
					substCost = GetSubstitutionCost(xItem, yItem, isComplete);
					op = EditOperation.Substitute;
				}

				double cost = distMatrix[i - 1, j - 1] + substCost;
				double min = cost;
				iPred = i - 1;
				jPred = j - 1;

				double delCost = usePrefixDelOp && j == GetCount(y) ? 0 : GetDeletionCost(xItem);
				cost = distMatrix[i - 1, j] + delCost;
				if (cost < min)
				{
					min = cost;
					iPred = i - 1;
					jPred = j;
					op = delCost == 0 ? EditOperation.PrefixDelete : EditOperation.Delete;
				}

				cost = distMatrix[i, j - 1] + GetInsertionCost(yItem);
				if (cost < min)
				{
					min = cost;
					iPred = i;
					jPred = j - 1;
					op = EditOperation.Insert;
				}

				return min;
			}

			if (i == 0 && j == 0)
			{
				iPred = 0;
				jPred = 0;
				op = EditOperation.None;
				return 0;
			}

			if (i == 0)
			{
				iPred = 0;
				jPred = j - 1;
				op = EditOperation.Insert;
				return distMatrix[0, j - 1] + GetInsertionCost(GetItem(y, j - 1));
			}

			iPred = i - 1;
			jPred = 0;
			op = EditOperation.Delete;
			return distMatrix[i - 1, 0] + GetDeletionCost(GetItem(x, i - 1));
		}
	}
}
