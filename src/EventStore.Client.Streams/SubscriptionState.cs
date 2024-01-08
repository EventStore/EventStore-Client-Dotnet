namespace EventStore.Client {
	/// <summary>
	/// An enumeration representing the state of a subscription.
	/// </summary>
	public enum SubscriptionState {
		/// <summary>
		/// Subscription is initializing
		/// </summary>
		Initializing = 0,
		/// <summary>
		/// Subscription has been successfully established
		/// </summary>
		Ok,
		/// <summary>
		/// Subscription has been terminated
		/// </summary>
		Disposed
	}
}
