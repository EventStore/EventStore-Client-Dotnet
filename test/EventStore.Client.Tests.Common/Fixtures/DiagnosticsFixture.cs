using System.Collections.Concurrent;
using System.Diagnostics;
using EventStore.Client.Diagnostics;
using EventStore.Diagnostics;
using EventStore.Diagnostics.Telemetry;
using EventStore.Diagnostics.Tracing;

namespace EventStore.Client.Tests;

[PublicAPI]
public class DiagnosticsFixture : EventStoreFixture {
	readonly ConcurrentDictionary<(string Operation, string Stream), List<Activity>> _activities = [];

	public DiagnosticsFixture() : base(x => x.RunProjections()) {
		var diagnosticActivityListener = new ActivityListener {
			ShouldListenTo = source => source.Name == EventStoreClientDiagnostics.InstrumentationName,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity => {
				var operation = (string?)activity.GetTagItem(TelemetryTags.Database.Operation);
				var stream    = (string?)activity.GetTagItem(TelemetryTags.EventStore.Stream);

				if (operation is null || stream is null)
					return;

				_activities.AddOrUpdate(
					(operation, stream),
					_ => [activity],
					(_, activities) => {
						activities.Add(activity);
						return activities;
					}
				);
			}
		};

		OnSetup = () => {
			ActivitySource.AddActivityListener(diagnosticActivityListener);
			return Task.CompletedTask;
		};

		OnTearDown = () => {
			diagnosticActivityListener.Dispose();
			return Task.CompletedTask;
		};
	}

	public List<Activity> GetActivitiesForOperation(string operation, string stream) =>
		_activities.TryGetValue((operation, stream), out var activities) ? activities : [];

	public void AssertAppendActivityHasExpectedTags(Activity activity, string stream) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryTags.Database.System, EventStoreClientDiagnostics.InstrumentationName },
			{ TelemetryTags.Database.Operation, TracingConstants.Operations.Append },
			{ TelemetryTags.EventStore.Stream, stream },
			{ TelemetryTags.Database.User, TestCredentials.Root.Username },
			{ TelemetryTags.Otel.StatusCode, ActivityStatusCodeHelper.OkStatusCodeTagValue }
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);
	}

	public void AssertErroneousAppendActivityHasExpectedTags(Activity activity, Exception actualException) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryTags.Otel.StatusCode, ActivityStatusCodeHelper.ErrorStatusCodeTagValue }
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);

		var actualEvent = activity.Events.ShouldHaveSingleItem();

		actualEvent.Name.ShouldBe(TelemetryTags.Exception.EventName);
		actualEvent.Tags.ShouldContain(
			new KeyValuePair<string, object?>(TelemetryTags.Exception.Type, actualException.GetType().FullName)
		);

		actualEvent.Tags.ShouldContain(
			new KeyValuePair<string, object?>(TelemetryTags.Exception.Message, actualException.Message)
		);

		actualEvent.Tags.Any(x => x.Key == TelemetryTags.Exception.Stacktrace).ShouldBeTrue();
	}

	public void AssertSubscriptionActivityHasExpectedTags(
		Activity activity,
		string stream,
		string eventId,
		string? subscriptionId = null
	) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryTags.Database.System, EventStoreClientDiagnostics.InstrumentationName },
			{ TelemetryTags.Database.Operation, TracingConstants.Operations.Subscribe },
			{ TelemetryTags.EventStore.Stream, stream },
			{ TelemetryTags.EventStore.EventId, eventId },
			{ TelemetryTags.EventStore.EventType, TestEventType },
			{ TelemetryTags.Database.User, TestCredentials.Root.Username }
		};

		if (subscriptionId != null)
			expectedTags[TelemetryTags.EventStore.SubscriptionId] = subscriptionId;

		foreach (var tag in expectedTags) {
			activity.Tags.ShouldContain(tag);
		}
	}
}
