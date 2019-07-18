using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.FastConsole
{
    public class FastJsonConsoleSink : ILogEventSink, IDisposable
    {
        private static readonly JsonValueFormatter ValueFormatter = new JsonValueFormatter();
        private static readonly StreamWriter ConsoleWriter = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding);
        private static readonly StringWriter BufferWriter = new StringWriter();
        private static readonly Channel<LogEvent> WriteQueue = Channel.CreateUnbounded<LogEvent>(new UnboundedChannelOptions { SingleReader = true });
        private static readonly Task WriteQueueWorker = WriteToConsoleStream();

        public void Emit(LogEvent logEvent) => WriteQueue.Writer.TryWrite(logEvent);

        private static async Task WriteToConsoleStream()
        {
            while (await WriteQueue.Reader.WaitToReadAsync())
                while (WriteQueue.Reader.TryRead(out var logEvent))
                {
                    if (logEvent != null)
                    {
                        // console output is a IO stream
                        // format and write event to in-memory buffer and then flush to console async
                        // do not use the locking textwriter from console.out used by console.writeline
                        Format(logEvent, BufferWriter);
                        await ConsoleWriter.WriteAsync(BufferWriter.ToString());
                        BufferWriter.GetStringBuilder().Clear();
                    }
                }

            await ConsoleWriter.FlushAsync();
        }

        private static void Format(LogEvent logEvent, TextWriter output)
        {
            output.Write("{\"timestamp\":\"");
            output.Write(logEvent.Timestamp.ToString("o"));

            output.Write("\",\"message\":");

            var message = logEvent.MessageTemplate.Render(logEvent.Properties);
            JsonValueFormatter.WriteQuotedJsonString(message, output);

            if (logEvent.Exception != null)
            {
                output.Write(",\"exception\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            if (logEvent.Properties.Count > 0)
            {
                output.Write(",\"properties\":{");

                var precedingDelimiter = "";
                foreach (var kvp in logEvent.Properties)
                {
                    output.Write(precedingDelimiter);
                    precedingDelimiter = ",";

                    JsonValueFormatter.WriteQuotedJsonString(kvp.Key, output);
                    output.Write(':');

                    ValueFormatter.Format(kvp.Value, output);
                }

                output.Write('}');
            }

            output.Write("\",\"level\":\"");
            output.Write(WriteLogLevel(logEvent.Level));

            output.Write('}');
            output.WriteLine();
        }

        private static string WriteLogLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose: return "VERBOSE";
                case LogEventLevel.Debug: return "DEBUG";
                case LogEventLevel.Information: return "INFO";
                case LogEventLevel.Warning: return "WARNING";
                case LogEventLevel.Error: return "ERROR";
                case LogEventLevel.Fatal: return "FATAL";
                default: return "INFO";
            }
        }

        public void Dispose()
        {
            // close write queue and wait until items are drained
            // then wait for all console output to be flushed
            WriteQueue.Writer.Complete();
            WriteQueueWorker.GetAwaiter().GetResult();
        }
    }
}
