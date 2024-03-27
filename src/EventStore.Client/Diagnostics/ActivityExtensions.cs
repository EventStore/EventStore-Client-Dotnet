using System.Diagnostics;
using System.Runtime.CompilerServices;
using EventStore.Client.Diagnostics.OpenTelemetry;
using EventStore.Client.Diagnostics.Tracing;

namespace EventStore.Client.Diagnostics;

static class ActivityExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TracingMetadata GetTracingMetadata(this Activity activity)
		=> new(activity.TraceId.ToString(), activity.SpanId.ToString(), activity.ParentSpanId.ToString());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Activity SetActivityStatus(this Activity activity, ActivityStatus activityStatus) {
		var (activityStatusCode, description, exception) = activityStatus;

		var statusCode = activityStatusCode switch {
			ActivityStatusCode.Error => "ERROR",
			ActivityStatusCode.Ok    => "OK",
			_                        => "UNSET"
		};

		activity.SetStatus(activityStatusCode, description);
		activity.SetTag(SemanticAttributes.OtelStatusCode, statusCode);
		activity.SetTag(SemanticAttributes.OtelStatusDescription, description);

		return exception != null
			? activity.SetException(exception)
			: activity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Activity SetException(this Activity activity, Exception exception) {
		var tags = new ActivityTagsCollection {
			{ SemanticAttributes.ExceptionType, exception.GetType().Name },
			{ SemanticAttributes.ExceptionMessage, $"{exception.Message} {exception.InnerException?.Message}" },
			{ SemanticAttributes.ExceptionStacktrace, exception.StackTrace }
		};

		foreach (var tag in tags) {
			activity.SetTag(tag.Key, tag.Value);
		}

		return activity.AddEvent(new ActivityEvent(SemanticAttributes.ExceptionEventName, DateTimeOffset.Now, tags));
	}
}
