using System.Diagnostics;
using EventStore.Diagnostics;
using EventStore.Diagnostics.Telemetry;
using EventStore.Diagnostics.Tracing;

namespace EventStore.Client.Diagnostics;

static class ActivitySourceExtensions {
	public static async ValueTask<T> TraceClientOperation<T>(
		this ActivitySource source,
		Func<ValueTask<T>> tracedOperation,
		string operationName,
		ActivityTagsCollection? tags = null
	) {
		using var activity = StartActivity(source, operationName, ActivityKind.Client, tags, Activity.Current?.Context);

		try {
			var res = await tracedOperation().ConfigureAwait(false);
			activity?.StatusOk();
			return res;
		}
		catch (Exception ex) {
			activity?.StatusError(ex);
			throw;
		}
	}

	public static void TraceSubscriptionEvent(
		this ActivitySource source,
		string? subscriptionId,
		ResolvedEvent resolvedEvent,
		ChannelInfo channelInfo,
		EventStoreClientSettings settings,
		UserCredentials? userCredentials
	) {
		if (resolvedEvent.OriginalEvent.ContentType != Constants.Metadata.ContentTypes.ApplicationJson)
			return;

		if (source.HasNoActiveListeners())
			return;

		var parentContext = resolvedEvent.OriginalEvent.Metadata.ExtractPropagationContext();

		if (parentContext is null) return;

		var tags = new ActivityTagsCollection()
			.WithRequiredTag(TelemetryTags.EventStore.Stream, resolvedEvent.OriginalEvent.EventStreamId)
			.WithOptionalTag(TelemetryTags.EventStore.SubscriptionId, subscriptionId)
			.WithRequiredTag(TelemetryTags.EventStore.EventId, resolvedEvent.OriginalEvent.EventId.ToString())
			.WithRequiredTag(TelemetryTags.EventStore.EventType, resolvedEvent.OriginalEvent.EventType)
			// Ensure consistent server.address attribute when connecting to cluster via dns discovery
			.WithGrpcChannelServerTags(channelInfo)
			.WithClientSettingsServerTags(settings)
			.WithOptionalTag(TelemetryTags.Database.User, userCredentials?.Username ?? settings.DefaultCredentials?.Username);

		StartActivity(source, TracingConstants.Operations.Subscribe, ActivityKind.Consumer, tags, parentContext)?.Dispose();
	}

	static Activity? StartActivity(
		this ActivitySource source,
		string operationName, ActivityKind activityKind, ActivityTagsCollection? tags = null, ActivityContext? parentContext = null
	) {
		if (source.HasNoActiveListeners())
			return null;

		(tags ??= new ActivityTagsCollection())
			.WithRequiredTag(TelemetryTags.Database.System, "eventstoredb")
			.WithRequiredTag(TelemetryTags.Database.Operation, operationName);

		return source
			.CreateActivity(
				operationName,
				activityKind,
				parentContext ?? default,
				tags,
				idFormat: ActivityIdFormat.W3C
			)
			?.Start();
	}

	static bool HasNoActiveListeners(this ActivitySource source) => !source.HasListeners();
}