using System;

namespace SIL.HermitCrab
{
	public class InvalidShapeException : Exception
	{
		private readonly string _str;
		private readonly int _position;

		public InvalidShapeException(string str, int position)
			: base(string.Format("The string '{0}' cannot be converted to a shape, position: {1}.", str, position))
		{
			_str = str;
			_position = position;
		}

		public string String
		{
			get { return _str; }
		}

		public int Position
		{
			get { return _position; }
		}
	}
}
