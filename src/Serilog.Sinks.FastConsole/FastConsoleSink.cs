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
        private readonly StreamWriter _consoleWriter = new(Console.OpenStandardOutput(), Console.OutputEncoding) { AutoFlush = true };
        private readonly StringWriter _bufferWriter = new();
        private readonly Channel<LogEvent?> _writeQueue;
        private readonly Task _writeQueueWorker;

        private readonly FastConsoleSinkOptions _options;
        private readonly MessageTemplateTextFormatter? _messageTemplateTextFormatter;
        private readonly JsonValueFormatter _valueFormatter = new();

        public FastConsoleSink(FastConsoleSinkOptions options, MessageTemplateTextFormatter? messageTemplateTextFormatter)
        {
            _options = options;
            _messageTemplateTextFormatter = messageTemplateTextFormatter;

            _writeQueue = options.QueueLimit > 0
                ? Channel.CreateBounded<LogEvent?>(new BoundedChannelOptions(options.QueueLimit.Value) { SingleReader = true })
                : Channel.CreateUnbounded<LogEvent?>(new UnboundedChannelOptions { SingleReader = true });

            _writeQueueWorker = WriteToConsoleStream();
        }

        // logs are immediately queued to channel
        public void Emit(LogEvent logEvent) => _writeQueue.Writer.TryWrite(logEvent);

        private async Task WriteToConsoleStream()
        {
            // cache reference to stringbuilder inside writer
            var sb = _bufferWriter.GetStringBuilder();

            while (await _writeQueue.Reader.WaitToReadAsync())
            while (_writeQueue.Reader.TryRead(out var logEvent))
            {
                if (logEvent == null) continue;

                // console output is an IO stream
                // format and write event to in-memory buffer and then flush to console async
                // do not use the locking textwriter from console.out used by console.writeline

                if (_options.UseJson)
                    RenderJson(logEvent, _bufferWriter);
                else
                    RenderText(logEvent, _bufferWriter);

#if NET5_0
                // use stringbuilder internal buffers directly without allocating a new string
                foreach (var chunk in sb.GetChunks())
                    await _consoleWriter.WriteAsync(chunk);
#else
                // fallback to creating string output 
                await _consoleWriter.WriteAsync(sb.ToString());
#endif

                sb.Clear();
            }

            await _consoleWriter.FlushAsync();
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
                _options.CustomJsonWriter(e, writer);
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

                        _valueFormatter.Format(kvp.Value, writer);
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

        private bool _disposed = false; // to detect redundant calls

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // close write queue and wait until items are drained
                // then wait for all console output to be flushed
                _writeQueue.Writer.Complete();
                _writeQueueWorker.Wait();
                _writeQueueWorker.Dispose();

                _bufferWriter.Dispose();
                _consoleWriter.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}