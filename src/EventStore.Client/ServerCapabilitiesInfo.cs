namespace EventStore.Client {
#pragma warning disable 1591
	public record ServerCapabilitiesInfo(
#pragma warning restore 1591
		bool SupportsBatchAppend = false,
		bool SupportsPersistentSubscriptionsToAll = false);
}
