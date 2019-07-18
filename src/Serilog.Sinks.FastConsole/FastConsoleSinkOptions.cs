using System;
using System.IO;
using Serilog.Events;

namespace Serilog.Sinks.FastConsole
{
    public class FastConsoleSinkOptions
    {
        /// <summary>
        /// Writes log event as a JSON object with { timestamp, message, level } properties.
        /// Default is true.
        /// </summary>
        public bool UseJson { get; set; } = true;

        /// <summary>
        /// Custom write method for controlling the JSON object output. You must write the entire object.
        /// Default is null, which will use built-in formatter.
        /// </summary>
        public Action<LogEvent, TextWriter> CustomJsonWriter { get; set; }
    }
}
