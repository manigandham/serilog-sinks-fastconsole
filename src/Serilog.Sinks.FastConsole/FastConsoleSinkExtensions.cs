using System;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.FastConsole
{
    public static class FastConsoleSinkExtensions
    {
        public static LoggerConfiguration FastConsole(
            this LoggerSinkConfiguration loggerConfiguration,
            FastConsoleSinkOptions? sinkOptions = null,
            string? outputTemplate = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch? loggingLevelSwitch = null)
        {
            sinkOptions ??= new FastConsoleSinkOptions();
            var messageTemplateTextFormatter = !String.IsNullOrWhiteSpace(outputTemplate) ? new MessageTemplateTextFormatter(outputTemplate) : null;

            var sink = new FastConsoleSink(sinkOptions, messageTemplateTextFormatter);

            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel, loggingLevelSwitch);
        }
    }
}