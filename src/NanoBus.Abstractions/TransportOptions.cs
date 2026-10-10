namespace NanoBus.Abstractions;

/// <summary>
/// Specifies transport options.
/// </summary>
public class TransportOptions
{
    /// <summary>
    /// The maximum number if times to retry message delivery.
    /// </summary>
    public int MaxDeliveryAttempts { get; set; } = 4;
}
