using System;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.FastConsole
{
    public static class FastConsoleSinkExtensions
    {
        public static LoggerConfiguration FastConsole(
            this LoggerSinkConfiguration loggerConfiguration,
            FastConsoleSinkOptions? sinkOptions = null,
            string? outputTemplate = null,
            ITextFormatter? textFormatter = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch? loggingLevelSwitch = null)
        {
            sinkOptions ??= new FastConsoleSinkOptions();

            // create text formatter if only output template is specified
            if (!String.IsNullOrWhiteSpace(outputTemplate) && textFormatter == null)
                textFormatter = new MessageTemplateTextFormatter(outputTemplate);

            var sink = new FastConsoleSink(sinkOptions, textFormatter);

            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel, loggingLevelSwitch);
        }
    }
}