// ReSharper disable CheckNamespace

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kurrent.Diagnostics.Telemetry;
using Kurrent.Diagnostics.Tracing;

namespace Kurrent.Diagnostics;

static class ActivityExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TracingMetadata GetTracingMetadata(this Activity activity) =>
		new(activity.TraceId.ToString(), activity.SpanId.ToString());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Activity StatusOk(this Activity activity, string? description = null) =>
		activity.SetActivityStatus(ActivityStatus.Ok(description));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Activity StatusError(this Activity activity, Exception exception) =>
		activity.SetActivityStatus(ActivityStatus.Error(exception));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Activity RecordException(this Activity activity, Exception? exception) {
		if (exception is null) return activity;

		var ex = exception is AggregateException aex ? aex.Flatten() : exception;

		var tags = new ActivityTagsCollection {
			{ TelemetryTags.Exception.Type, ex.GetType().FullName },
			{ TelemetryTags.Exception.Stacktrace, ex.ToInvariantString() }
		};

		if (!string.IsNullOrWhiteSpace(exception.Message))
			tags.Add(TelemetryTags.Exception.Message, ex.Message);

		activity.AddEvent(new ActivityEvent(TelemetryTags.Exception.EventName, default, tags));

		return activity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Activity SetActivityStatus(this Activity activity, ActivityStatus status) {
		var statusCode = ActivityStatusCodeHelper.GetTagValueForStatusCode(status.StatusCode);

		activity.SetStatus(status.StatusCode, status.Description);
		activity.SetTag(TelemetryTags.Otel.StatusCode, statusCode);
		activity.SetTag(TelemetryTags.Otel.StatusDescription, status.Description);

		return activity.IsAllDataRequested ? activity.RecordException(status.Exception) : activity;
	}
}
