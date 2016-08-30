using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Common
{
    public class ProcessRunner : IDisposable
    {
        private Process _process;
        private List<string> _envpath = new List<string>();

        public Process Process { get { return _process; } }

        public LogSeverity LogLevel { get; set; }

        public string WorkingDirectory { get; set; }

        public string FileName { get; set; }

        public string Arguments { get; set; }

        public event DataReceivedEventHandler OnOutput;

        public ProcessRunner()
        {
            LogLevel = LogSeverity.output;
        }

        public void Start()
        {
            var info = new ProcessStartInfo();
            info.WorkingDirectory = WorkingDirectory;
            info.FileName = FileName;
            info.Arguments = Arguments;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;

            InstallEnvironmentPath(info);

            SetupProcessInfo(info);

            _process = new Process();
            _process.StartInfo = info;

            SetupIO();

            _process.Start();

            StartIO();
        }

        private void InstallEnvironmentPath(ProcessStartInfo info)
        {
            var pathstring = info.EnvironmentVariables["PATH"] ?? "";
            var pathlist = pathstring.Split(';');

            /*foreach (var path in pathlist)
            {
                Log.Debug("  {0}", path);
            }*/ 
        }

        protected virtual void SetupProcessInfo(ProcessStartInfo info)
        {
            info.RedirectStandardError = true;
        }

        protected virtual void StartIO()
        {
            _process.EnableRaisingEvents = true;
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();
        }

        protected virtual void SetupIO()
        {
            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
        }

        public int Wait()
        {
            _process.WaitForExit();
            return _process.ExitCode;
        }

        private void ProcessData(DataReceivedEventArgs e)
        {
            if (null != e.Data)
            {
                if (null == OnOutput)
                {
                    var log = Log.Create();
                    log.Header = e.Data;
                    log.Severity = LogLevel;
                    log.Submit();
                }
                else
                {
                    OnOutput(this, e);
                }
            }
        }

        public void Terminate()
        {
            try
            {
                _process.Kill();
            }
            catch (Exception) { }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ProcessData(e);
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            ProcessData(e);
        }

        public void Dispose()
        {
            if(null != _process)
            {
                _process.Dispose();
                _process = null;
            }
        }
    }
}
