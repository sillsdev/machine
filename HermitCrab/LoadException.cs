using System;

namespace SIL.HermitCrab
{
	/// <summary>
	/// The specific error type of morph exception. This is useful for displaying
	/// user-friendly error messages.
	/// </summary>
	public enum LoadError
	{
		/// <summary>
		/// An object is not defined.
		/// </summary>
		UndefinedObject,
		/// <summary>
		/// A error occurred while parsing the configuration.
		/// </summary>
		ParseError,
		/// <summary>
		/// A phonological rule has an unknown subrule type.
		/// </summary>
		InvalidSubruleType,
		/// <summary>
		/// The configuration is in the incorrect format.
		/// </summary>
		InvalidFormat,
		/// <summary>
		/// A string cannot be converted to a shape.
		/// </summary>
		InvalidShape
	}

	/// <summary>
	/// This exception is thrown during the load process. It is used
	/// to indicate loading and configuration parsing errors.
	/// </summary>
	public class LoadException : Exception
	{
		private readonly LoadError _errorType;

		public LoadException(LoadError errorType)
		{
			_errorType = errorType;
		}

		public LoadException(LoadError errorType, string message)
			: base(message)
		{
			_errorType = errorType;
		}

		public LoadException(LoadError errorType, string message, Exception inner)
			: base(message, inner)
		{
			_errorType = errorType;
		}

		public LoadError ErrorType
		{
			get
			{
				return _errorType;
			}
		}
	}
}
