using System;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This exception is thrown by Loaders during the load process. It is used
	/// to indicate loading and configuration parsing errors.
	/// </summary>
	public class LoadException : Exception
	{
		/// <summary>
		/// The specific error type of morph exception. This is useful for displaying
		/// user-friendly error messages.
		/// </summary>
		public enum LoadErrorType
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
			/// Too many feature values were defined.
			/// </summary>
			TooManyFeatureValues,
			/// <summary>
			/// A character definition table could not translate a phonetic shape used in a rule.
			/// </summary>
			InvalidRuleShape,
			/// <summary>
			/// A character definition table could not translate a phonetic shape used in a lexical entry.
			/// </summary>
			InvalidEntryShape,
			/// <summary>
			/// A phonological rule has an unknown subrule type.
			/// </summary>
			InvalidSubruleType,
			/// <summary>
			/// The configuration is in the incorrect format.
			/// </summary>
			InvalidFormat,
			/// <summary>
			/// There is no current morpher selected in the loader.
			/// </summary>
			NoCurrentMorpher
		}

		private readonly LoadErrorType _errorType;
		private readonly Loader _loader;

		public LoadException(LoadErrorType errorType, Loader loader)
		{
			_errorType = errorType;
			_loader = loader;
		}

		public LoadException(LoadErrorType errorType, Loader loader, string message)
			: base(message)
		{
			_errorType = errorType;
			_loader = loader;
		}

		public LoadException(LoadErrorType errorType, Loader loader, string message, Exception inner)
			: base(message, inner)
		{
			_errorType = errorType;
			_loader = loader;
		}

		public LoadErrorType ErrorType
		{
			get
			{
				return _errorType;
			}
		}

		public Loader Loader
		{
			get
			{
				return _loader;
			}
		}
	}
}
