namespace NanoBus.Abstractions;

/// <summary>
/// The structure of messages in the DLQ.
/// </summary>
/// <param name="UniqueId">The unique id used for processing.</param>
/// <param name="MessageType">The type of message.</param>
/// <param name="Message">The message.</param>
/// <param name="Method">The method of either Send or Publish.</param>
/// <param name="Context">The message context.</param>
/// <param name="DeliveryAttempts">The number of times delivery has been attempted.</param>
/// <param name="LastFailedOn">The time the message last failed.</param>
/// <param name="NextRetryTime">The next time message delivery will be attempted.</param>
/// <param name="LastError">The last error experienced during delivery.</param>
/// <param name="Token">The cancellation token to apply to the redelivery of the message.</param>
public record FailedMessage(
    string UniqueId, 
    string MessageType, 
    IMessage Message, 
    MessageMethod Method, 
    MessageContext Context, 
    int DeliveryAttempts, 
    DateTime LastFailedOn, 
    DateTime NextRetryTime,
    Exception? LastError, 
    CancellationToken Token
    );
