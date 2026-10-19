namespace NanoBus.Abstractions;

/// <summary>
/// The context of the message being delivered.
/// </summary>
/// <param name="transport">The instance of the transport used to deliver the message.</param>
/// <param name="messageId">The message id of the message being delivered delivered.</param>
/// <param name="correlationId">The correlation id of the message being delivered.</param>
/// <param name="headers">The headers of the message being delivered.</param>
public class MessageContext(ITransport transport, string? messageId = null, string? correlationId = null, Dictionary<string, string>? headers = null)
{
    /// <summary>
    /// The headers of the message being delivered.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; } = headers ?? [];

    /// <summary>
    /// The message id of the message being delivered.
    /// </summary>
    public string MessageId { get; } = messageId ?? Guid.NewGuid().ToString("N");

    /// <summary>
    /// The correlation id of the message being delivered.
    /// </summary>
    public string CorrelationId { get; } = correlationId ?? Guid.NewGuid().ToString("N");

    private readonly ITransport _transport = transport;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <param name="metadata"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask SendAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        await _transport.SendAsync(message, metadata, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <param name="metadata"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask PublishAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        await _transport.PublishAsync(message, metadata, cancellationToken)
            .ConfigureAwait(false);
    }
}
