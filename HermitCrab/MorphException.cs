using System;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This exception is thrown during the morphing process. It is used to indicate
	/// that an error occurred while morphing a word.
	/// </summary>
	public class MorphException : Exception
	{
		/// <summary>
		/// The specific error type of morph exception. This is useful for displaying
		/// user-friendly error messages.
		/// </summary>
		public enum MorphErrorType
		{
			/// <summary>
			/// A character definition table could not translate a phonetic shape.
			/// </summary>
			InvalidShape,
			/// <summary>
			/// A feature is uninstantiated when a rule requires that it agree between the target and environment.
			/// </summary>
			UninstantiatedFeature,
			/// <summary>
			/// A phonetic shape contains too many segments.
			/// </summary>
			TooManySegs
		}

		private readonly MorphErrorType _errorType;

		public MorphException(MorphErrorType errorType)
		{
			_errorType = errorType;
		}

		public MorphException(MorphErrorType errorType, string message)
			: base(message)
		{
			_errorType = errorType;
		}

		public MorphException(MorphErrorType errorType, string message, Exception inner)
			: base(message, inner)
		{
			_errorType = errorType;
		}

		public MorphErrorType ErrorType
		{
			get
			{
				return _errorType;
			}
		}
	}
}
