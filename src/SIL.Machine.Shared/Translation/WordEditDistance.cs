namespace SIL.Machine.Translation
{
	internal class WordEditDistance : EditDistanceBase<string, char>
	{
		private double _hitCost;
		private double _insertionCost;
		private double _deletionCost;
		private double _substitutionCost;

		public double HitCost
		{
			get { return _hitCost; }
			set { _hitCost = value; }
		}

		public double InsertionCost
		{
			get { return _insertionCost; }
			set { _insertionCost = value; }
		}

		public double DeletionCost
		{
			get { return _deletionCost; }
			set { _deletionCost = value; }
		}

		public double SubstitutionCost
		{
			get { return _substitutionCost; }
			set { _substitutionCost = value; }
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
			return _hitCost;
		}

		protected override double GetSubstitutionCost(char x, char y, bool isComplete)
		{
			return _substitutionCost;
		}

		protected override double GetDeletionCost(char x)
		{
			return _deletionCost;
		}

		protected override double GetInsertionCost(char y)
		{
			return _insertionCost;
		}

		protected override bool IsHit(char x, char y, bool isComplete)
		{
			return x == y;
		}
	}
}
