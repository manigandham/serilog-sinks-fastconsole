using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
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
        private readonly ITextFormatter? _textFormatter;
        private readonly JsonValueFormatter _valueFormatter = new();

        public FastConsoleSink(FastConsoleSinkOptions options, ITextFormatter? textFormatter)
        {
            _options = options;
            _textFormatter = textFormatter;

            // support obsolete CustomJsonWriter
            if (_textFormatter == null && options.CustomJsonWriter != null)
                _textFormatter = new ObsoleteJsonWriter(options.CustomJsonWriter);

            _writeQueue = options.QueueLimit > 0
                ? Channel.CreateBounded<LogEvent?>(new BoundedChannelOptions(options.QueueLimit.Value)
                    {SingleReader = true})
                : Channel.CreateUnbounded<LogEvent?>(new UnboundedChannelOptions {SingleReader = true});

            _writeQueueWorker = Task.Run(WriteToConsoleStream);
        }

        // logs are immediately queued to channel
        public void Emit(LogEvent logEvent) => _writeQueue.Writer.TryWrite(logEvent);

        private async Task WriteToConsoleStream()
        {
            // cache reference to stringbuilder inside writer
            var sb = _bufferWriter.GetStringBuilder();

            while (await _writeQueue.Reader.WaitToReadAsync().ConfigureAwait(false))
            while (_writeQueue.Reader.TryRead(out var logEvent))
            {
                if (logEvent == null) continue;

                // console output is an IO stream
                // format and write event to in-memory buffer and then flush to console async
                // do not use the locking textwriter from console.out used by console.writeline

                if (_textFormatter != null)
                    _textFormatter.Format(logEvent, _bufferWriter);
                else if (_options.UseJson)
                    RenderJson(logEvent, _bufferWriter);
                else
                    RenderText(logEvent, _bufferWriter);

#if NET5_0_OR_GREATER
                // use stringbuilder internal buffers directly without allocating a new string
                foreach (var chunk in sb.GetChunks())
                    await _consoleWriter.WriteAsync(chunk).ConfigureAwait(false);
#else
                // fallback to creating string output
                await _consoleWriter.WriteAsync(sb.ToString()).ConfigureAwait(false);
#endif

                sb.Clear();
            }

            await _consoleWriter.FlushAsync().ConfigureAwait(false);
        }

        private void RenderText(LogEvent e, StringWriter writer)
        {
            writer.Write(e.MessageTemplate.Render(e.Properties));
            writer.WriteLine();
        }

        private void RenderJson(LogEvent e, StringWriter writer)
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
                _writeQueueWorker.GetAwaiter().GetResult();

                _bufferWriter.Dispose();
                _consoleWriter.Dispose();
            }

            _disposed = true;
        }

        private class ObsoleteJsonWriter : ITextFormatter
        {
            private readonly Action<LogEvent, TextWriter>? _method;

            public ObsoleteJsonWriter(Action<LogEvent, TextWriter>? method)
            {
                _method = method;
            }

            public void Format(LogEvent e, TextWriter w) => _method?.Invoke(e, w);
        }

        #endregion
    }
}