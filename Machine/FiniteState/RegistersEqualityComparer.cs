using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.FiniteState
{
	internal class RegistersEqualityComparer<TOffset> : IEqualityComparer<NullableValue<TOffset>[,]>
	{
		private readonly IEqualityComparer<TOffset> _offsetComparer;

		public RegistersEqualityComparer(IEqualityComparer<TOffset> offsetComparer)
		{
			_offsetComparer = offsetComparer;
		}

		public IEqualityComparer<TOffset> OffsetComparer
		{
			get { return _offsetComparer; }
		}

		public bool Equals(NullableValue<TOffset>[,] x, NullableValue<TOffset>[,] y)
		{
			for (int i = 0; i < x.GetLength(0); i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (x[i, j].HasValue != y[i, j].HasValue)
						return false;

					if (x[i, j].HasValue && !_offsetComparer.Equals(x[i, j].Value, x[i, j].Value))
						return false;
				}
			}
			return true;
		}

		public int GetHashCode(NullableValue<TOffset>[,] obj)
		{
			int code = 23;
			for (int i = 0; i < obj.GetLength(0); i++)
			{
				for (int j = 0; j < 2; j++)
					code = code * 31 + (obj[i, j].HasValue && obj[i, j].Value != null ? _offsetComparer.GetHashCode(obj[i, j].Value) : 0);
			}
			return code;
		}
	}
}
