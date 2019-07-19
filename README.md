# Serilog.Sinks.FastConsole

Serilog sink that writes to console, focused on high-performance non-blocking output. This sink supports both plaintext and JSON output to the console but it **does not support themes and colors**. 

The default console sink uses heavy inefficient code and writes with Console.WriteLine which introduces a global lock for each line. This sink uses a [background channel](https://ndportmann.com/system-threading-channels/) instead with a single text buffer and async writes to remove all blocking and contention to the console output stream. It also implements IDiposable so you can call `Log.CloseAndFlush();` to ensure all lines are flushed.

Release notes here: [CHANGELOG.md](CHANGELOG.md)

## Getting started

#### Install [package from Nuget](https://www.nuget.org/packages/Serilog.Sinks.FastConsole/):

```
dotnet add package Serilog.Sinks.FastConsole
```

#### Configure Logger (using code):

```csharp
var config = new FastConsoleSinkOptions { UseJson = true };
Log.Logger = new LoggerConfiguration().WriteTo.FastConsole(config).CreateLogger();
```

## Sink Options

Name | Default | Description
---- | ------- | -----------
`UseJson` | true | Whether to write as a plaintext line (using any configured output format) or as a JSON object with `{ timestamp, level, message, properties }` structure.
`CustomJsonWriter` | null | `Action<LogEvent, TextWriter>` delegate for any custom method to write JSON (if JSON output is enabled).
