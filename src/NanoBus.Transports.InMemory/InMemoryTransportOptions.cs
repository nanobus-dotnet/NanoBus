using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// Options for the in-memory transport.
/// </summary>
public class InMemoryTransportOptions : TransportOptions
{
    /// <summary>
    /// The maximum capacity for each message type channel.
    /// </summary>
    public int MaxChannelCapacity { get; set; } = 100;

    /// <summary>
    /// The amount of time to throttle polling for DLQ messages.
    /// </summary>
    public TimeSpan DeadLetterPollingDelay { get; set; } = TimeSpan.Parse("00:00:01");
}
