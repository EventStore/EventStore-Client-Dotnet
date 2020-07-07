namespace EventStore.Client {
	/// <summary>
	/// Represents the reason subscription drop happened.
	/// </summary>
	public enum SubscriptionDroppedReason {
		/// <summary>
		/// Subscription dropped because the subscription was disposed.
		/// </summary>
		Disposed,
		/// <summary>
		/// Subscription dropped because of an error in user code.
		/// </summary>
		SubscriberError,
		/// <summary>
		/// Subscription dropped because of a server error.
		/// </summary>
		ServerError
	}
}
