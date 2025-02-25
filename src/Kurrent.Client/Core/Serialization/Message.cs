using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

/// <summary>
/// Represents a message wrapper in the KurrentDB system, containing both domain data and optional metadata.
/// Messages can represent events, commands, or other domain objects along with their associated metadata.
/// </summary>
/// <param name="Data">The message domain data.</param>
/// <param name="Metadata">Optional metadata providing additional context about the message, such as correlation IDs, timestamps, or user information.</param>
/// <param name="MessageId">Unique identifier for this specific message instance. When null, the system will auto-generate an ID.</param>
public record Message(object Data, object? Metadata, Uuid? MessageId = null) {
	/// <summary>
	/// Creates a new Message with the specified domain data and message ID, but without metadata.
	/// This factory method is a convenient shorthand when working with systems that don't require metadata.
	/// </summary>
	/// <param name="data">The message domain data.</param>
	/// <param name="messageId">Unique identifier for this message instance. Must not be Uuid.Empty.</param>
	/// <returns>A new immutable Message instance containing the provided data and ID with null metadata.</returns>
	/// <example>
	/// <code>
	/// // Create a message with a specific ID
	/// var userCreated = new UserCreated { Id = "123", Name = "Alice" };
	/// var messageId = Uuid.NewUuid();
	/// var message = Message.From(userCreated, messageId);
	/// </code>
	/// </example>
	public static Message From(object data, Uuid messageId) =>
		From(data, null, messageId);

	/// <summary>
	/// Creates a new Message with the specified domain data and message ID and metadata.
	/// </summary>
	/// <param name="data">The message domain data.</param>
	/// <param name="metadata">Optional metadata providing additional context about the message, such as correlation IDs, timestamps, or user information.</param>
	/// <param name="messageId">Unique identifier for this specific message instance. </param>
	/// <returns>A new immutable Message instance with the specified properties.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when messageId is explicitly set to Uuid.Empty, which is an invalid identifier.</exception>
	/// <example>
	/// <code>
	/// // Create a message with data and metadata
	/// var orderPlaced = new OrderPlaced { OrderId = "ORD-123", Amount = 99.99m };
	/// var metadata = new EventMetadata { 
	///     UserId = "user-456", 
	///     Timestamp = DateTimeOffset.UtcNow,
	///     CorrelationId = correlationId
	/// };
	/// 
	/// // Let the system assign an ID automatically
	/// var message = Message.From(orderPlaced, metadata);
	/// 
	/// // Or specify a custom ID
	/// var messageWithId = Message.From(orderPlaced, metadata, Uuid.NewUuid());
	/// </code>
	/// </example>
	public static Message From(object data, object? metadata = null, Uuid? messageId = null) {
		if (messageId == Uuid.Empty)
			throw new ArgumentOutOfRangeException(nameof(messageId), "Message ID cannot be an empty UUID.");

		return new Message(data, metadata, messageId);
	}
}
