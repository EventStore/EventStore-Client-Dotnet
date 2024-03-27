using System.Diagnostics;
using EventStore.Client.Diagnostics.OpenTelemetry;
using JetBrains.Annotations;

namespace EventStore.Client.Diagnostics;

/// <summary>
/// Controls diagnostic instrumentation of the client.
/// </summary>
public static class EventStoreClientDiagnostics {
	static readonly ActivitySource         _activitySource = new ActivitySource(ActivitySourceName);
	static readonly ActivityTagsCollection _defaultTags    = [new(SemanticAttributes.DatabaseSystem, "eventstoredb")];

	internal const string ActivitySourceName = "eventstoredb.client";

	/// <summary>
	/// Indicates if diagnostics are enabled, which is true by default.
	/// Diagnostics can be disabled via the <see cref="Disable"/> method or alternatively
	/// by setting the environment variable 'EVENTSTORE_DISABLE_DIAGNOSTICS' to 'true'.
	/// </summary>
	public static bool Enabled { get; private set; } =
		Environment.GetEnvironmentVariable("EVENTSTORE_DISABLE_DIAGNOSTICS") != "true";

	/// <summary>
	/// Programatically disables client diagnostics.
	/// </summary>
	[PublicAPI]
	public static void Disable() => Enabled = false;

	internal static Activity? StartActivity(string operation, ActivityTagsCollection? tags) {
		if (!Enabled) return default;

		var activityName = $"{ActivitySourceName}.{operation}";

		return _activitySource.CreateActivity(
			activityName,
			ActivityKind.Client,
			parentContext: default,
			new ActivityTagsCollection {
					{ SemanticAttributes.DatabaseOperation, activityName }
				}
				.WithTags(_defaultTags)
				.WithTags(tags),
			idFormat: ActivityIdFormat.W3C
		)?.Start();
	}
}
