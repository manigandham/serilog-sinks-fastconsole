# Serilog.Sinks.FastConsole

Serilog sink that writes to console with high-performance non-blocking output. Supports plaintext and JSON output but **does not support themes and colors**.

This sink uses a [background channel](https://ndportmann.com/system-threading-channels/) with a single text buffer and async writes to remove all blocking and lock contention to the console output stream. It also implements `IDiposable` so that calling `Log.CloseAndFlush()` will ensure all lines are flushed. Recommended for high-volume logging where console logging is captured and sent elsewhere, for example this library was originally created for a large fleet of API servers running in Kubernetes and logging hundreds of large JSON objects every second.

Built for `net6.0`, `net5.0`, `netstandard2.1`, `netstandard2.0`

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

| Name         | Default | Description                                                                                                                                                                                                |
| ------------ | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `UseJson`    | true    | Enable to write log events as a JSON object with `{ timestamp, level, message, properties }` structure. Provide an `ITextFormatter` implementation instead if you want to customize the output completely. |
| `QueueLimit` | null    | Set max limit for the number of log entries queued in memory. Used to provide backpressure and avoid out-of-memory issues for high-volume logging.                                                         |
