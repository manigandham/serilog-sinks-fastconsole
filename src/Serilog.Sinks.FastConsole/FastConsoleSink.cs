using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.FastConsole
{
    public class FastConsoleSink : ILogEventSink, IDisposable
    {
        private readonly JsonValueFormatter ValueFormatter = new JsonValueFormatter();
        private readonly StreamWriter ConsoleWriter = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding) { AutoFlush = true };
        private readonly StringWriter BufferWriter = new StringWriter();
        private readonly Channel<LogEvent> WriteQueue;
        private readonly Task WriteQueueWorker;

        private readonly FastConsoleSinkOptions _options;
        private readonly MessageTemplateTextFormatter _messageTemplateTextFormatter;

        public FastConsoleSink(FastConsoleSinkOptions options, MessageTemplateTextFormatter messageTemplateTextFormatter)
        {
            _options = options;
            _messageTemplateTextFormatter = messageTemplateTextFormatter;

            if (options.QueueLimit > 0)
                WriteQueue = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(options.QueueLimit.Value) { SingleReader = true });
            else
                WriteQueue = Channel.CreateUnbounded<LogEvent>(new UnboundedChannelOptions { SingleReader = true });

            WriteQueueWorker = WriteToConsoleStream();
        }

        // logs are immediately queued to channel
        public void Emit(LogEvent logEvent) => WriteQueue.Writer.TryWrite(logEvent);

        private async Task WriteToConsoleStream()
        {
            while (await WriteQueue.Reader.WaitToReadAsync())
            while (WriteQueue.Reader.TryRead(out var logEvent))
            {
                if (logEvent != null)
                {
                    // console output is a IO stream
                    // format and write event to in-memory buffer and then flush to console async
                    // do not use the locking textwriter from console.out used by console.writeline

                    if (_options.UseJson)
                        RenderJson(logEvent, BufferWriter);
                    else
                        RenderText(logEvent, BufferWriter);

                    await ConsoleWriter.WriteAsync(BufferWriter.ToString());
                    BufferWriter.GetStringBuilder().Clear();
                }
            }

            await ConsoleWriter.FlushAsync();
        }

        private void RenderText(LogEvent e, StringWriter writer)
        {
            if (_messageTemplateTextFormatter != null)
            {
                _messageTemplateTextFormatter.Format(e, writer);
            }
            else
            {
                writer.Write(e.MessageTemplate.Render(e.Properties));
                writer.WriteLine();
            }
        }

        private void RenderJson(LogEvent e, StringWriter writer)
        {
            if (_options.CustomJsonWriter != null)
            {
                _options.CustomJsonWriter.Invoke(e, writer);
            }
            else
            {
                writer.Write("{");

                writer.Write("\"timestamp\":\"");
                writer.Write(e.Timestamp.ToString("o"));

                writer.Write("\",\"level\":\"");
                writer.Write(WriteLogLevel(e.Level));

                writer.Write("\",\"message\":");
                var message = e.MessageTemplate.Render(e.Properties);
                JsonValueFormatter.WriteQuotedJsonString(message, writer);

                if (e.Exception != null)
                {
                    writer.Write(",\"exception\":");
                    JsonValueFormatter.WriteQuotedJsonString(e.Exception.ToString(), writer);
                }

                if (e.Properties.Count > 0)
                {
                    writer.Write(",\"properties\":{");

                    var precedingDelimiter = "";
                    foreach (var kvp in e.Properties)
                    {
                        writer.Write(precedingDelimiter);
                        precedingDelimiter = ",";

                        JsonValueFormatter.WriteQuotedJsonString(kvp.Key, writer);
                        writer.Write(':');

                        ValueFormatter.Format(kvp.Value, writer);
                    }

                    writer.Write('}');
                }

                writer.Write('}');
                writer.WriteLine();
            }
        }

        private static string WriteLogLevel(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => "VERBOSE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARNING",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "FATAL",
            _ => "INFO",
        };

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // close write queue and wait until items are drained
                    // then wait for all console output to be flushed
                    WriteQueue.Writer.Complete();
                    WriteQueueWorker.GetAwaiter().GetResult();

                    BufferWriter.Dispose();
                    ConsoleWriter.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
