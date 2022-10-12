namespace Serilog.Sinks.FastConsole;

/// <summary>
/// Configuration options for <see cref="FastConsoleSink"/> .
/// </summary>
public class FastConsoleSinkOptions
{
    /// <summary>
    /// Set max limit for the number of log entries queued in memory. Used to provide
    /// backpressure and avoid out-of-memory issues for high-volume logging.
    /// Default is <see langword="null"/> for unbounded queue.
    /// </summary>
    public int? QueueLimit { get; set; }

    /// <summary>
    /// Sets the behavior of sink when queue of messages is full. Set to <see langword="true"/>
    /// to block when log queue is full to avoid losing log messages if blocking is preferred.
    /// Only applicable if <see cref="QueueLimit"/> is set.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool BlockWhenFull { get; set; }

    /// <summary>
    /// Writes log event as a JSON object with <c>{ timestamp, level, message, properties }</c> structure.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool UseJson { get; set; } = true;
}
