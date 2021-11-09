#nullable enable
namespace EventStore.Client {
	/// <summary />
	public record ServerCapabilities(string? ServerVersion = null, bool SupportsBatchAppend = false,
		bool SupportsPersistentSubscriptionsToAll = false);
}
