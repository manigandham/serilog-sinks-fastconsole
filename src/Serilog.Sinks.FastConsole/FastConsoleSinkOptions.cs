using System;
using System.IO;
using Serilog.Events;

namespace Serilog.Sinks.FastConsole
{
    public class FastConsoleSinkOptions
    {
        /// <summary>
        /// Set max limit for the number of log entries queued in memory. Used to provide backpressure and avoid out-of-memory issues for high-volume logging.
        /// Default is null for unbounded queue.
        /// </summary>
        public int? QueueLimit { get; set; }

        /// <summary>
        /// Writes log event as a JSON object with <c>{ timestamp, level, message, properties }</c> structure.
        /// Default is true.
        /// </summary>
        public bool UseJson { get; set; } = true;

        [Obsolete("Use a class that implements ITextFormatter instead. This is the official way to override the text output for Serilog. This method is still supported (using a wrapper class) but will be removed in a future release.")]
        public Action<LogEvent, TextWriter>? CustomJsonWriter { get; set; }
    }
}