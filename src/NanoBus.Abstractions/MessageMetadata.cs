namespace NanoBus.Abstractions;

/// <summary>
/// The class used to specify optional metadata properties to pass with messages.
/// </summary>
public class MessageMetadata
{
    /// <summary>
    /// The message id of the message.
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// The correlation id of the message; used to group related messages.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// The custom headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = [];
}
