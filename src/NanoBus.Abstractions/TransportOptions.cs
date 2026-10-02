namespace NanoBus.Abstractions;

/// <summary>
/// Specifies transport options.
/// </summary>
public class TransportOptions
{
    /// <summary>
    /// The maximum number if times to retry message delivery.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
