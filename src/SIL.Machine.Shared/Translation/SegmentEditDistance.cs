using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	internal class SegmentEditDistance : EditDistanceBase<string[], string>
	{
		private readonly WordEditDistance _wordEditDistance;

		public SegmentEditDistance()
		{
			_wordEditDistance = new WordEditDistance();
		}

		public double HitCost
		{
			get { return _wordEditDistance.HitCost; }
			set { _wordEditDistance.HitCost = value; }
		}

		public double SubstitutionCost
		{
			get { return _wordEditDistance.SubstitutionCost; }
			set { _wordEditDistance.SubstitutionCost = value; }
		}

		public double InsertionCost
		{
			get { return _wordEditDistance.InsertionCost; }
			set { _wordEditDistance.InsertionCost = value; }
		}

		public double DeletionCost
		{
			get { return _wordEditDistance.DeletionCost; }
			set { _wordEditDistance.DeletionCost = value; }
		}

		public double ComputePrefix(string[] x, string[] y, bool isLastItemComplete, bool usePrefixDelOp,
			out IEnumerable<EditOperation> wordOps, out IEnumerable<EditOperation> charOps)
		{
			double[,] distMatrix;
			double dist = Compute(x, y, isLastItemComplete, usePrefixDelOp, out distMatrix);

			charOps = null;
			int i = x.Length;
			int j = y.Length;
			var ops = new Stack<EditOperation>();
			while (i > 0 || j > 0)
			{
				EditOperation op;
				ProcessMatrixCell(x, y, distMatrix, usePrefixDelOp, j != y.Length || isLastItemComplete, i, j, out i,
					out j, out op);
				if (op != EditOperation.PrefixDelete)
					ops.Push(op);

				if (j + 1 == y.Length && !isLastItemComplete && op == EditOperation.Hit)
					_wordEditDistance.ComputePrefix(x[i], y[y.Length - 1], true, true, out charOps);
			}

			wordOps = ops;
			if (charOps == null)
				charOps = Enumerable.Empty<EditOperation>();

			return dist;
		}

		public void IncrComputePrefixFirstRow(List<double> scores, List<double> prevScores, string[] yIncr)
		{
			if (scores != prevScores)
			{
				scores.Clear();
				foreach (double score in prevScores)
					scores.Add(score);
			}

			int startPos = scores.Count;
			for (int jIncr = 0; jIncr < yIncr.Length; jIncr++)
			{
				int j = startPos + jIncr;
				if (j == 0)
					scores.Add(GetInsertionCost(yIncr[jIncr]));
				else
					scores.Add(scores[j - 1] + GetInsertionCost(yIncr[jIncr]));
			}
		}

		public IEnumerable<EditOperation> IncrComputePrefix(List<double> scores, List<double> prevScores, string xWord,
			string[] yIncr, bool isLastItemComplete)
		{
			var x = new[] {xWord};
			var y = new string[prevScores.Count - 1];
			for (int i = 0; i < yIncr.Length; i++)
				y[prevScores.Count - yIncr.Length - 1 + i] = yIncr[i];

			double[,] distMatrix = InitDistMatrix(x, y);

			for (int j = 0; j < prevScores.Count; j++)
				distMatrix[0, j] = prevScores[j];
			for (int j = 0; j < scores.Count; j++)
				distMatrix[1, j] = scores[j];

			while (scores.Count < prevScores.Count)
				scores.Add(0);

			int startPos = prevScores.Count - yIncr.Length;

			var ops = new List<EditOperation>();
			for (int jIncr = 0; jIncr < yIncr.Length; jIncr++)
			{
				int j = startPos + jIncr;
				int iPred, jPred;
				EditOperation op;
				double dist = ProcessMatrixCell(x, y, distMatrix, false, j != y.Length || isLastItemComplete,
					1, j, out iPred, out jPred, out op);
				scores[j] = dist;
				distMatrix[1, j] = dist;
				ops.Add(op);
			}

			return ops;
		}

		protected override int GetCount(string[] item)
		{
			return item.Length;
		}

		protected override string GetItem(string[] seq, int index)
		{
			return seq[index];
		}

		protected override double GetHitCost(string x, string y, bool isComplete)
		{
			return HitCost * y.Length;
		}

		protected override double GetSubstitutionCost(string x, string y, bool isComplete)
		{
			if (x == string.Empty)
				return (SubstitutionCost * 0.99) * y.Length;

			IEnumerable<EditOperation> ops;
			if (isComplete)
				_wordEditDistance.Compute(x, y, out ops);
			else
				_wordEditDistance.ComputePrefix(x, y, true, true, out ops);

			int hitCount, insCount, substCount, delCount;
			GetOpCounts(ops, out hitCount, out insCount, out substCount, out delCount);

			return (HitCost * hitCount) + (InsertionCost * insCount) + (SubstitutionCost * substCount)
				+ (DeletionCost * delCount);
		}

		private static void GetOpCounts(IEnumerable<EditOperation> ops, out int hitCount, out int insCount,
			out int substCount, out int delCount)
		{
			hitCount = 0;
			insCount = 0;
			substCount = 0;
			delCount = 0;
			foreach (EditOperation op in ops)
			{
				switch (op)
				{
					case EditOperation.Hit:
						hitCount++;
						break;
					case EditOperation.Insert:
						insCount++;
						break;
					case EditOperation.Substitute:
						substCount++;
						break;
					case EditOperation.Delete:
						delCount++;
						break;
				}
			}
		}

		protected override double GetDeletionCost(string x)
		{
			if (x == string.Empty)
				return DeletionCost;
			return DeletionCost * x.Length; 
		}

		protected override double GetInsertionCost(string y)
		{
			return InsertionCost * y.Length;
		}

		protected override bool IsHit(string x, string y, bool isComplete)
		{
			return x == y || (!isComplete && x.StartsWith(y));
		}
	}
}
