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
    public int MaxChannelCapacity { get; set; } = 1000;
}
