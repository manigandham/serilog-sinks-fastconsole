using System;
using Serilog.Configuration;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.FastConsole
{
    public static class FastConsoleSinkExtensions
    {
        public static LoggerConfiguration FastConsole(this LoggerSinkConfiguration loggerConfiguration,
          FastConsoleSinkOptions sinkOptions = null,
          string outputTemplate = null)
        {
            sinkOptions ??= new FastConsoleSinkOptions();
            var messageTemplateTextFormatter = String.IsNullOrWhiteSpace(outputTemplate) ? null : new MessageTemplateTextFormatter(outputTemplate, null);

            var sink = new FastConsoleSink(sinkOptions, messageTemplateTextFormatter);

            return loggerConfiguration.Sink(sink);
        }
    }
}
