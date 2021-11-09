namespace EventStore.Client {
#pragma warning disable 1591
	public record ServerCapabilities(
		bool SupportsBatchAppend = false,
		bool SupportsPersistentSubscriptionsToAll = false);
}
