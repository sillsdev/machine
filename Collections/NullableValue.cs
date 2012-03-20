namespace SIL.Collections
{
	public struct NullableValue<T>
	{
		private T _value;
		private bool _hasValue;

		public NullableValue(T value)
		{
			_value = value;
			_hasValue = true;
		}

		public bool HasValue
		{
			get { return _hasValue; }
			set
			{
				_hasValue = value;
				if (!_hasValue)
					_value = default(T);
			}
		}

		public T Value
		{
			get { return _value; }
			set
			{
				_value = value;
				_hasValue = true;
			}
		}

		public override string ToString()
		{
			if (_hasValue)
				return _value.ToString();
			return "null";
		}
	}
}
