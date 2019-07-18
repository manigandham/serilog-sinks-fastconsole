using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.FastConsole
{
    public class SimpleConsoleSink : ILogEventSink, IDisposable
    {
        private static readonly StreamWriter ConsoleWriter = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding) { AutoFlush = true };
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
            output.WriteLine(logEvent.MessageTemplate.Render(logEvent.Properties));
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
