using System;

namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when the EventStoreDB drops a persistent subscription.
	/// </summary>
	public class PersistentSubscriptionDroppedByServerException : Exception {
		/// <summary>
		/// The stream name.
		/// </summary>
		public readonly string StreamName;

		/// <summary>
		/// The group name.
		/// </summary>
		public readonly string GroupName;

		/// <summary>
		/// Constructs a new <see cref="PersistentSubscriptionDroppedByServerException"/>.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="groupName"></param>
		/// <param name="exception"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public PersistentSubscriptionDroppedByServerException(string streamName, string groupName,
			Exception? exception = null)
			: base($"Subscription group '{groupName}' on stream '{streamName}' was dropped.", exception) {
			if (streamName == null) throw new ArgumentNullException(nameof(streamName));
			if (groupName == null) throw new ArgumentNullException(nameof(groupName));
			StreamName = streamName;
			GroupName = groupName;
		}
	}
}
