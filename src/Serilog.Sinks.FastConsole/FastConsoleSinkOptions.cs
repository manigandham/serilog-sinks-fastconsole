namespace Serilog.Sinks.FastConsole;

/// <summary>
/// Configuration options for <see cref="FastConsoleSink"/> .
/// </summary>
public class FastConsoleSinkOptions
{
    /// <summary>
    /// Set max limit for the number of log entries queued in memory. 
    /// Used to provide backpressure and avoid out-of-memory issues for high-volume logging.
    /// Default is <see langword="null"/> for unbounded queue.
    /// </summary>
    public int? QueueLimit { get; set; }

    /// <summary>
    /// Set behavior when queue is full. When <see langword="true"/> this will block the thread until logs are output to console.
    /// Only applicable if <see cref="QueueLimit"/> is set.
    /// Default is <see langword="false"/> to drop logs when queue is full.
    /// </summary>
    public bool BlockWhenFull { get; set; }

    /// <summary>
    /// Writes log event as a JSON object with <c>{ timestamp, level, message, properties }</c> structure.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool UseJson { get; set; } = true;
}
