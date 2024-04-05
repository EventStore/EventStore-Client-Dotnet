namespace EventStore.Client;
#pragma warning disable 1591
public record ServerCapabilities(
	bool SupportsBatchAppend = false,
	bool SupportsPersistentSubscriptionsToAll = false,
	bool SupportsPersistentSubscriptionsGetInfo = false,
	bool SupportsPersistentSubscriptionsRestartSubsystem = false,
	bool SupportsPersistentSubscriptionsReplayParked = false,
	bool SupportsPersistentSubscriptionsList = false
);