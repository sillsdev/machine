using System;

namespace SIL.HermitCrab
{
	/// <summary>
	/// The specific error type of morph exception. This is useful for displaying
	/// user-friendly error messages.
	/// </summary>
	public enum MorphErrorCode
	{
		/// <summary>
		/// A feature is uninstantiated when a rule requires that it agree between the target and environment.
		/// </summary>
		UninstantiatedFeature,
		/// <summary>
		/// A phonetic shape contains too many segments.
		/// </summary>
		TooManySegs
	}

	/// <summary>
	/// This exception is thrown during the morphing process. It is used to indicate
	/// that an error occurred while morphing a word.
	/// </summary>
	public class MorphException : Exception
	{
		private readonly MorphErrorCode _errorCode;

		public MorphException(MorphErrorCode errorCode)
		{
			_errorCode = errorCode;
		}

		public MorphException(MorphErrorCode errorCode, string message)
			: base(message)
		{
			_errorCode = errorCode;
		}

		public MorphException(MorphErrorCode errorCode, string message, Exception inner)
			: base(message, inner)
		{
			_errorCode = errorCode;
		}

		public MorphErrorCode ErrorCode
		{
			get
			{
				return _errorCode;
			}
		}
	}
}
