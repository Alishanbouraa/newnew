// OfflinePOS.Core/Diagnostics/XamlTraceListener.cs
using System;
using System.Diagnostics;

namespace OfflinePOS.Core.Diagnostics
{
    /// <summary>
    /// Trace listener for XAML diagnostics that logs through the debug output
    /// </summary>
    public class XamlTraceListener : TraceListener
    {
        private const string LogPrefix = "[XAML Trace]";

        /// <summary>
        /// Initializes a new instance of the XamlTraceListener class
        /// </summary>
        public XamlTraceListener()
        {
        }

        /// <summary>
        /// Writes a message to the trace log
        /// </summary>
        /// <param name="message">Message to write</param>
        public override void Write(string message)
        {
            Debug.Write($"{LogPrefix}: {message}");
        }

        /// <summary>
        /// Writes a message to the trace log with a newline
        /// </summary>
        /// <param name="message">Message to write</param>
        public override void WriteLine(string message)
        {
            Debug.WriteLine($"{LogPrefix}: {message}");
        }

        /// <summary>
        /// Writes a message to the trace log with a specified category
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="category">Message category</param>
        public override void Write(string message, string category)
        {
            Debug.Write($"{LogPrefix} [{category}]: {message}");
        }

        /// <summary>
        /// Writes a message to the trace log with a specified category and a newline
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="category">Message category</param>
        public override void WriteLine(string message, string category)
        {
            Debug.WriteLine($"{LogPrefix} [{category}]: {message}");
        }
    }
}