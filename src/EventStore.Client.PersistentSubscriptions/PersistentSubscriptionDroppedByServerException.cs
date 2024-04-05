namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when the EventStoreDB drops a persistent subscription.
/// </summary>
public class PersistentSubscriptionDroppedByServerException : Exception {
	/// <summary>
	/// The group name.
	/// </summary>
	public readonly string GroupName;

	/// <summary>
	/// The stream name.
	/// </summary>
	public readonly string StreamName;

	/// <summary>
	/// Constructs a new <see cref="PersistentSubscriptionDroppedByServerException"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException"></exception>
	public PersistentSubscriptionDroppedByServerException(
		string streamName, string groupName,
		Exception? exception = null
	)
		: base($"Subscription group '{groupName}' on stream '{streamName}' was dropped.", exception) {
		StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
		GroupName  = groupName ?? throw new ArgumentNullException(nameof(groupName));
	}
}