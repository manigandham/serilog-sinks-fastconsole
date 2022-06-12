namespace Serilog.Sinks.FastConsole;

public class FastConsoleSinkOptions
{
    /// <summary>
    /// Set max limit for the number of log entries queued in memory. Used to provide backpressure and avoid out-of-memory issues for high-volume logging.
    /// Default is null for unbounded queue.
    /// </summary>
    public int? QueueLimit { get; set; }

    /// <summary>
    /// Writes log event as a JSON object with <c>{ timestamp, level, message, properties }</c> structure.
    /// Default is true.
    /// </summary>
    public bool UseJson { get; set; } = true;
}
