namespace EventStore.Client;

/// <summary>
/// The base record of all stream messages.
/// </summary>
public abstract record PersistentSubscriptionMessage {
	/// <summary>
	/// A <see cref="PersistentSubscriptionMessage"/> that represents a <see cref="EventStore.Client.ResolvedEvent"/>.
	/// </summary>
	/// <param name="ResolvedEvent">The <see cref="EventStore.Client.ResolvedEvent"/>.</param>
	/// <param name="RetryCount">The number of times the <see cref="EventStore.Client.ResolvedEvent"/> has been retried.</param>
	public record Event(ResolvedEvent ResolvedEvent, int? RetryCount) : PersistentSubscriptionMessage;

	/// <summary>
	/// A <see cref="PersistentSubscriptionMessage"/> representing a stream that was not found.
	/// </summary>
	public record NotFound : PersistentSubscriptionMessage {
		internal static readonly NotFound Instance = new();
	}

	/// <summary>
	/// A <see cref="PersistentSubscriptionMessage"/> indicating that the subscription is ready to send additional messages.
	/// </summary>
	/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
	public record SubscriptionConfirmation(string SubscriptionId) : PersistentSubscriptionMessage;

	/// <summary>
	/// A <see cref="PersistentSubscriptionMessage"/> that could not be identified, usually indicating a lower client compatibility level than the server supports.
	/// </summary>
	public record Unknown : PersistentSubscriptionMessage {
		internal static readonly Unknown Instance = new();
	}
}