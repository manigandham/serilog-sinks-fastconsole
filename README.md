# Serilog.Sinks.FastConsole

Serilog sink that writes to console, focused on high-performance non-blocking output. 

The default console sink uses heavy inefficient code and writes with Console.WriteLine which introduces a global lock for each line. This sink uses a [background channel](https://ndportmann.com/system-threading-channels/) instead with a single text buffer and async writes to remove all blocking and contention to the console output stream. It also implements IDiposable so you can call `Log.CloseAndFlush();` to ensure all lines are flushed.

This sink supports both plaintext and JSON output to the console but it **does not support themes and colors**.



