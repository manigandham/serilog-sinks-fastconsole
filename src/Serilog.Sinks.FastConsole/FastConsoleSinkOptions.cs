using System;
using System.IO;
using Serilog.Events;

namespace Serilog.Sinks.FastConsole
{
    public class FastConsoleSinkOptions
    {
        public bool UseJson { get; set; } = true;
        public Action<LogEvent, TextWriter> CustomJsonWriter { get; set; }
    }
}
