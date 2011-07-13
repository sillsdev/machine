using System.Collections.Generic;

namespace System
{
	public static class Tuple
	{
		public static Tuple<T1> Create<T1>(T1 item1)
		{
			return new Tuple<T1>(item1);
		}

		public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
		{
			return new Tuple<T1, T2>(item1, item2);
		}

		public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
		{
			return new Tuple<T1, T2, T3>(item1, item2, item3);
		}

		public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
		{
			return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
		}
	}

	public class Tuple<T1>
	{
		private readonly T1 _item1;

		public Tuple(T1 item1)
		{
			_item1 = item1;
		}

		public T1 Item1
		{
			get { return _item1; }
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			var tuple = obj as Tuple<T1>;
			if (tuple == null)
				return false;
			return EqualityComparer<T1>.Default.Equals(_item1, tuple._item1);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T1>.Default.GetHashCode(_item1);
		}

		public override string ToString()
		{
			return string.Format("({0})", _item1);
		}
	}

	public class Tuple<T1, T2>
	{
		private readonly T1 _item1;
		private readonly T2 _item2;

		public Tuple(T1 item1, T2 item2)
		{
			_item1 = item1;
			_item2 = item2;
		}

		public T1 Item1
		{
			get { return _item1; }
		}

		public T2 Item2
		{
			get { return _item2; }
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			var tuple = obj as Tuple<T1, T2>;
			if (tuple == null)
				return false;
			return EqualityComparer<T1>.Default.Equals(_item1, tuple._item1) && EqualityComparer<T2>.Default.Equals(_item2, tuple._item2);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T1>.Default.GetHashCode(_item1) ^ EqualityComparer<T2>.Default.GetHashCode(_item2);
		}

		public override string ToString()
		{
			return string.Format("({0}, {1})", _item1, _item2);
		}
	}

	public class Tuple<T1, T2, T3>
	{
		private readonly T1 _item1;
		private readonly T2 _item2;
		private readonly T3 _item3;

		public Tuple(T1 item1, T2 item2, T3 item3)
		{
			_item1 = item1;
			_item2 = item2;
			_item3 = item3;
		}

		public T1 Item1
		{
			get { return _item1; }
		}

		public T2 Item2
		{
			get { return _item2; }
		}

		public T3 Item3
		{
			get { return _item3; }
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			var tuple = obj as Tuple<T1, T2, T3>;
			if (tuple == null)
				return false;
			return EqualityComparer<T1>.Default.Equals(_item1, tuple._item1) && EqualityComparer<T2>.Default.Equals(_item2, tuple._item2)
				&& EqualityComparer<T3>.Default.Equals(_item3, tuple._item3);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T1>.Default.GetHashCode(_item1) ^ EqualityComparer<T2>.Default.GetHashCode(_item2)
				^ EqualityComparer<T3>.Default.GetHashCode(_item3);
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}, {2})", _item1, _item2, _item3);
		}
	}

	public class Tuple<T1, T2, T3, T4>
	{
		private readonly T1 _item1;
		private readonly T2 _item2;
		private readonly T3 _item3;
		private readonly T4 _item4;

		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4)
		{
			_item1 = item1;
			_item2 = item2;
			_item3 = item3;
			_item4 = item4;
		}

		public T1 Item1
		{
			get { return _item1; }
		}

		public T2 Item2
		{
			get { return _item2; }
		}

		public T3 Item3
		{
			get { return _item3; }
		}

		public T4 Item4
		{
			get { return _item4; }
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			var tuple = obj as Tuple<T1, T2, T3, T4>;
			if (tuple == null)
				return false;
			return EqualityComparer<T1>.Default.Equals(_item1, tuple._item1) && EqualityComparer<T2>.Default.Equals(_item2, tuple._item2)
				&& EqualityComparer<T3>.Default.Equals(_item3, tuple._item3) && EqualityComparer<T4>.Default.Equals(_item4, tuple._item4);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T1>.Default.GetHashCode(_item1) ^ EqualityComparer<T2>.Default.GetHashCode(_item2)
				^ EqualityComparer<T3>.Default.GetHashCode(_item3) ^ EqualityComparer<T4>.Default.GetHashCode(_item4);
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}, {2}, {3})", _item1, _item2, _item3, _item4);
		}
	}
}
