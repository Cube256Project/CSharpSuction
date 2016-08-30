using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpSuction
{
    /// <summary>
    /// Emits a suction.
    /// </summary>
    /// <remarks>
    /// <para>Emit implementations derive from this class.</para>
    /// </remarks>
    public abstract class Emit
    {
        #region Private

        private string _destination = string.Empty;
        private string _output = string.Empty;
        private List<string> _outputs = new List<string>();

        #endregion

        #region Properties

        public string OutputBaseDirectory { get; set; }

        /// <summary>
        /// Path of binary output, relative to OutputBaseDirectory.
        /// </summary>
        public string OutputDirectory
        {
            get
            {
                return null == _output ? OutputBaseDirectory :
                    Path.IsPathRooted(_output) ? _output :
                    Path.Combine(OutputBaseDirectory, _output);
            }

            set
            {
                /*if (Path.IsPathRooted(value))
                {
                    throw new ArgumentException("rooted path not allowed for 'OutputDirectory'.");
                }*/ 

                _output = value;
            }
        }

        /// <summary>
        /// Original source directory.
        /// </summary>
        public string OriginalDirectory { get; set; }

        /// <summary>
        /// Destination (for project file) inside the output directory.
        /// </summary>
        /// <remarks>
        /// <para>Must use relative path to set.</para>
        /// <para>Returns absolute path.</para>
        /// </remarks>
        public string DestinationDirectory
        {
            get
            {
                return null == _destination ? OutputBaseDirectory : Path.Combine(OutputBaseDirectory, _destination);
            }

            set
            {
                if (Path.IsPathRooted(value))
                {
                    throw new ArgumentException("rooted path not allowed for 'DestinationDirectory'.");
                }

                _destination = value;
            }
        }

        public IEnumerable<string> Outputs {  get { return _outputs; } }

        public long OutputBytes
        {
            get
            {
                return _outputs.Sum(e => new FileInfo(e).Length);
            }
        }

        protected Suction Suction { get; private set; }

        #endregion

        #region Events

        public event SuctionEventHandler OnMessage;

        #endregion

        #region Diagnostics

        protected void Trace(string format, params object[] args)
        {
            var message = string.Format(format, args);
            Suction.Results.Debug("{0}", message);
            TriggerMessage(message);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates the result from the suction.
        /// </summary>
        /// <param name="suction"></param>
        /// <returns></returns>
        public bool Generate(Suction suction)
        {
            try
            {
                Suction = suction;
                CreateDirectories();
                return Generate();
            }
            finally
            {
                Suction = null;
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Provides the implementation.
        /// </summary>
        protected abstract bool Generate();

        protected void AddOutput(string outputfile)
        {
            _outputs.Add(outputfile);
        }

        protected void TriggerMessage(string message)
        {
            if(null != OnMessage)
            {
                var e = new SuctionEventArgs();
                e.Code = SuctionEventCode.Message;
                e.Message = message;
                OnMessage(this, e);
            }
        }

        #endregion

        #region Private Methods

        private void CreateDirectories()
        {
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(DestinationDirectory);
        }

        #endregion
    }
}
