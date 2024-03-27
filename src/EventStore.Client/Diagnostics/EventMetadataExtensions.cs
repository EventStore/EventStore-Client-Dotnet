#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Diagnostics;
using System.Runtime.CompilerServices;
using EventStore.Client.Diagnostics.Tracing;
using Google.Protobuf.Collections;

namespace EventStore.Client.Diagnostics;

public static class EventMetadataExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IDictionary<string, string> InjectTracingMetadata(
		this IDictionary<string, string> eventMetadata
	) {
		if (Activity.Current == null) return eventMetadata;

		var tracingMetadata = Activity.Current.GetTracingMetadata();

		return eventMetadata
			.AddIfNotNull(TracingConstants.Metadata.TraceId, tracingMetadata.TraceId)
			.AddIfNotNull(TracingConstants.Metadata.SpanId, tracingMetadata.SpanId)
			.AddIfNotNull(TracingConstants.Metadata.ParentSpanId, tracingMetadata.ParentSpanId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityContext? RestoreTracingContext(this MapField<string, string> eventMetadata) {
		var (traceId, spanId, _) = eventMetadata.ExtractTracingMetadata();

		if (traceId == null || spanId == null) return default;

		try {
			return new(
				ActivityTraceId.CreateFromString(traceId.ToCharArray()),
				ActivitySpanId.CreateFromString(spanId.ToCharArray()),
				ActivityTraceFlags.Recorded,
				isRemote: true
			);
		} catch (Exception) {
			return default;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static TracingMetadata ExtractTracingMetadata(this MapField<string, string> eventMetadata)
		=> new(
			eventMetadata[TracingConstants.Metadata.TraceId],
			eventMetadata[TracingConstants.Metadata.SpanId],
			eventMetadata[TracingConstants.Metadata.ParentSpanId]
		);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static IDictionary<string, string> AddIfNotNull(
		this IDictionary<string, string> eventMetadata, string key, string? value
	) {
		if (string.IsNullOrEmpty(value)) return eventMetadata;

		eventMetadata[key] = value!;
		return eventMetadata;
	}
}
