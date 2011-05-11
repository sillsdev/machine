using System.Collections.Generic;
using System.Text;
using SIL.APRE;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class is the abstract class that all Loader classes inherit from.
    /// Loaders are responsible for parsing the input configuration file, loading
    /// Morphers, and running commands.
    /// </summary>
    public abstract class Loader
    {
        private Morpher _curMorpher;
        private readonly IDBearerSet<Morpher> _morphers;
        private bool _quitOnError = true;
		private IOutput _output;
        private bool _isLoaded;
        private bool _traceInputs = true;

    	protected Loader()
        {
            _morphers = new IDBearerSet<Morpher>();
        }

		/// <summary>
		/// Gets the default output encoding.
		/// </summary>
		/// <value>The default output encoding.</value>
		public abstract Encoding DefaultOutputEncoding
		{
			get;
		}

        /// <summary>
        /// Gets all loaded morphers.
        /// </summary>
        /// <value>All morphers.</value>
        public IEnumerable<Morpher> Morphers
        {
            get
            {
                return _morphers;
            }
        }

        /// <summary>
        /// Gets the current morpher.
        /// </summary>
        /// <value>The current morpher.</value>
        public Morpher CurrentMorpher
        {
            get
            {
                return _curMorpher;
            }
        }

		/// <summary>
		/// Gets or sets the output.
		/// </summary>
		/// <value>The output.</value>
        public IOutput Output
        {
            get
            {
                return _output;
            }

            set
            {
                _output = value;
            }
        }

        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }
        }

        public bool QuitOnError
        {
            get
            {
                return _quitOnError;
            }

            set
            {
                _quitOnError = value;
            }
        }

        /// <summary>
        /// Gets the morpher associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The morpher.</returns>
        public Morpher GetMorpher(string id)
        {
            Morpher morpher;
            if (_morphers.TryGetValue(id, out morpher))
                return morpher;
            return null;
        }

        /// <summary>
        /// Loads the specified config file and runs all commands.
        /// </summary>
        /// <param name="configFile">The config file.</param>
        public abstract void Load(string configFile);

		public abstract void Load();

        public virtual void Reset()
        {
            _curMorpher = null;
            _morphers.Clear();
            _isLoaded = false;
        }

        protected void MorphAndLookupWord(string word, bool prettyPrint)
        {
			if (_output != null)
				_output.MorphAndLookupWord(_curMorpher, word, prettyPrint, _traceInputs);
        }

		protected LoadException CreateUndefinedObjectException(string message, string id)
		{
			var le = new LoadException(LoadException.LoadErrorType.UndefinedObject, this, message);
			le.Data["id"] = id;
			return le;
		}
    }

}