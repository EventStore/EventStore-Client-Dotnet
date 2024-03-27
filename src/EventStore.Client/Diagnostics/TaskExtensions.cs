#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Diagnostics;

namespace EventStore.Client.Diagnostics;

public static class TaskExtensions {
	public static async ValueTask<T> Trace<T>(
		this ValueTask<T> tracedOperation, string operationName, ActivityTagsCollection? tags = null
	) {
		if (!EventStoreClientDiagnostics.Enabled) return await tracedOperation.ConfigureAwait(false);

		using var activity = EventStoreClientDiagnostics.StartActivity(operationName, tags);

		try {
			var res = await tracedOperation.ConfigureAwait(false);
			activity?.SetActivityStatus(ActivityStatus.Ok());
			return res;
		} catch (Exception ex) {
			activity?.SetActivityStatus(ActivityStatus.Error(ex));
			throw;
		}
	}
}
