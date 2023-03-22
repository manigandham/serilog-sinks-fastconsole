using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.FastConsole;

public class FastConsoleSink : ILogEventSink, IDisposable
{
    private static readonly StreamWriter _consoleWriter = new(Console.OpenStandardOutput(), Console.OutputEncoding, 4096, true);
    private readonly StringWriter _bufferWriter = new();
    private readonly Channel<LogEvent?> _queue;
    private readonly Task _worker;
    private readonly FastConsoleSinkOptions _options;
    private readonly ITextFormatter? _textFormatter;
    private readonly JsonValueFormatter _valueFormatter = new();
    private readonly bool _waitForQueue;

    public FastConsoleSink(FastConsoleSinkOptions options, ITextFormatter? textFormatter)
    {
        _options = options;
        _textFormatter = textFormatter;

        _waitForQueue = options.QueueLimit > 0 && options.BlockWhenFull == true;
        _queue = options.QueueLimit > 0
            ? Channel.CreateBounded<LogEvent?>(new BoundedChannelOptions(options.QueueLimit!.Value) { SingleReader = true, FullMode = _options.BlockWhenFull ? BoundedChannelFullMode.Wait : BoundedChannelFullMode.DropWrite })
            : Channel.CreateUnbounded<LogEvent?>(new UnboundedChannelOptions { SingleReader = true });

        // Use Task.Run instead of Task.Factory.StartNew since it unwraps Task<Task> into the just Task.
        _worker = Task.Run(WriteToConsoleStream, CancellationToken.None);
    }

    public void Emit(LogEvent logEvent)
    {
        // logs are immediately written to the underlying System.Threading.Channel which handles queue size and blocking automatically
        // `_waitForQueue` check is used to save task overhead when not blocking for writes
        if (!_queue.Writer.TryWrite(logEvent) && _waitForQueue)
            _queue.Writer.WriteAsync(logEvent).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task WriteToConsoleStream()
    {
        // cache reference to StringBuilder inside StringWriter
        var sb = _bufferWriter.GetStringBuilder();

        // console output is an IO stream, so logs are formatted to an in-memory buffer and then written to console asynchronously  
        // do not use the locking textwriter from console.out used by console.writeline

#if NET5_0_OR_GREATER
        await foreach (var logEvent in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
#else
        while (await _queue.Reader.WaitToReadAsync().ConfigureAwait(false))
        while (_queue.Reader.TryRead(out var logEvent))
#endif
        {
            if (logEvent == null) continue;
            if (_textFormatter != null) _textFormatter.Format(logEvent, _bufferWriter);
            else if (_options.UseJson) RenderJson(logEvent, _bufferWriter);
            else RenderText(logEvent, _bufferWriter);

#if NET5_0_OR_GREATER
            // use stringbuilder internal buffers directly without allocating a new string
            foreach (var chunk in sb.GetChunks())
                await _consoleWriter.WriteAsync(chunk).ConfigureAwait(false);
#else
            // fallback to creating string output
            await _consoleWriter.WriteAsync(sb.ToString()).ConfigureAwait(false);
#endif

            // must clear StringBuilder buffer manually
            sb.Clear();
        }

        await _consoleWriter.FlushAsync().ConfigureAwait(false);
    }

    private static void RenderText(LogEvent e, StringWriter writer)
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

            string? precedingDelimiter = null;
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

    private bool _disposed; // to detect redundant calls

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Close write queue and wait until items are drained
                // then wait for all console output to be flushed.
                // Use TryComplete instead of Complete because Complete throws
                // System.Threading.Channels.ChannelClosedException: 'The channel has been closed.'
                // in case of concurrent Dispose calls.
                _queue.Writer.TryComplete();
                _worker.ConfigureAwait(false).GetAwaiter().GetResult();
                _consoleWriter.Dispose();
            }

            _disposed = true;
        }
    }

    #endregion
}
