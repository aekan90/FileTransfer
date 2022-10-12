using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartPlug.WorkerService
{
    public class Logger : INotifyPropertyChanged, IDisposable
    {

        /// <summary>   The log. </summary>
        private StringBuilder _logs = new StringBuilder();
        private bool _enableFileLog { get; set; }
        StreamWriter _logFileStream;

        public Logger()
        {
            Add("Logger Started");
        }

        public Logger(bool WriteToFile, String LogsFolder = "")
        {
            _enableFileLog = WriteToFile;
            string logPath = "";
            string ts = GetTimestamp(DateTime.Now);
            string ws = "";
            try
            {
                if (!string.IsNullOrEmpty(LogsFolder))
                {
                    DirectoryInfo df = new DirectoryInfo(LogsFolder);
                    if (!df.Exists)
                    {
                        df.Create();
                    }
                    if (WriteToFile)
                    {
                        logPath = @"" + LogsFolder + "\\SmartPdu_Log_" + ts + ".txt";
                        _logFileStream = File.AppendText(@"" + LogsFolder + "\\SmartPdu_Log_" + ts + ".txt");
                        _logFileStream.AutoFlush = true;
                    }
                }
                else
                {
                    if (WriteToFile)
                    {
                        logPath = @"" + Environment.CurrentDirectory + "\\SmartPdu_Log_" + ts + ".txt";
                        _logFileStream = File.AppendText("SmartPdu_Log_" + ts + ".txt");
                        _logFileStream.AutoFlush = true;
                    }
                }
            }
            catch (Exception _ex)
            {
                if (WriteToFile)
                {
                    ws = "Requested logs folder is not available. " + _ex.Message;

                    logPath = @"" + Environment.CurrentDirectory + "\\SmartPdu_Log_" + ts + ".txt";
                    _logFileStream = File.AppendText("SmartPdu_Log_" + ts + ".txt");
                    _logFileStream.AutoFlush = true;
                }
            }


            Add("Logger Started ");
            if (!string.IsNullOrEmpty(ws))
            {
                Add("WARNING ! ");
                Add(ws);
            }
            Add("Log File available at : " + logPath);

        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the log. </summary>
        ///
        /// <value> The log. </value>
        ///-------------------------------------------------------------------------------------------------

        public String Log
        {
            get { return _logs.ToString(); }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Adds Value. </summary>
        ///
        /// <remarks>   Yusuf A. (Luhmann), 9/4/2019. </remarks>
        ///
        /// <param name="Value">    The value to add. </param>
        ///-------------------------------------------------------------------------------------------------

        internal void Add(string Value)
        {
            _logs.AppendLine(Value);
            if (this._enableFileLog)
            {
                _logFileStream.WriteLine(DateTime.Now + ": " + Value);
            }
            OnPropertyChanged("Log");
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Clears this object to its blank/initial state. </summary>
        ///
        /// <remarks>   Yusuf A. (Luhmann), 9/4/2019. </remarks>
        ///-------------------------------------------------------------------------------------------------

        public void Clear()
        {
            _logs.Clear();
            OnPropertyChanged("Log");
        }

        #region INotifyPropertyChanged
        /// <summary>   Occurs when a property value changes. </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Executes the property changed action. </summary>
        ///
        /// <remarks>   Yusuf A. (Luhmann), 9/4/2019. </remarks>
        ///
        /// <param name="propertyName"> (Optional) Name of the property. </param>
        ///-------------------------------------------------------------------------------------------------

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion


        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        private bool isDisposed = false;
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (_logFileStream != null)
                    {
                        _logFileStream.Close();
                        _logFileStream.Dispose();
                    }
                }
            }
            isDisposed = true;
        }
    }
}
