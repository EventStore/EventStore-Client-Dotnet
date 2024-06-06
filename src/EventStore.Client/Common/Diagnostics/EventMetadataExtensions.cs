using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using EventStore.Diagnostics;
using EventStore.Diagnostics.Tracing;

namespace EventStore.Client.Diagnostics;

static class EventMetadataExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> InjectTracingContext(this ReadOnlyMemory<byte> eventMetadata, Activity? activity) =>
		eventMetadata.InjectTracingMetadata(activity?.GetTracingMetadata() ?? TracingMetadata.None);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityContext? ExtractPropagationContext(this ReadOnlyMemory<byte> eventMetadata) =>
		eventMetadata.ExtractTracingMetadata().ToActivityContext(isRemote: true);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TracingMetadata ExtractTracingMetadata(this ReadOnlyMemory<byte> eventMetadata) {
		var reader = new Utf8JsonReader(eventMetadata.Span);
		
		if (!JsonDocument.TryParseValue(ref reader, out var doc)
		 || !doc.RootElement.TryGetProperty(TracingConstants.Metadata.TraceId, out var traceId)
		 || !doc.RootElement.TryGetProperty(TracingConstants.Metadata.SpanId, out var spanId))
			return TracingMetadata.None;

		return new TracingMetadata(traceId.GetString(), spanId.GetString());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ReadOnlySpan<byte> InjectTracingMetadata(this ReadOnlyMemory<byte> eventMetadata, TracingMetadata tracingMetadata) {
		if (tracingMetadata == TracingMetadata.None || !tracingMetadata.IsValid)
			return eventMetadata.Span;

		return eventMetadata.IsEmpty
			? JsonSerializer.SerializeToUtf8Bytes(tracingMetadata)
			: TryInjectTracingMetadata(eventMetadata, tracingMetadata).ToArray();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ReadOnlyMemory<byte> TryInjectTracingMetadata(this ReadOnlyMemory<byte> utf8Json, TracingMetadata tracingMetadata) {
		try {
			using var doc    = JsonDocument.Parse(utf8Json);
			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream);

			writer.WriteStartObject();

			if (doc.RootElement.ValueKind != JsonValueKind.Object)
				return utf8Json;

			foreach (var prop in doc.RootElement.EnumerateObject())
				prop.WriteTo(writer);

			writer.WritePropertyName(TracingConstants.Metadata.TraceId);
			writer.WriteStringValue(tracingMetadata.TraceId);
			writer.WritePropertyName(TracingConstants.Metadata.SpanId);
			writer.WriteStringValue(tracingMetadata.SpanId);

			writer.WriteEndObject();
			writer.Flush();

			return stream.ToArray();
		}
		catch (Exception) {
			return utf8Json;
		}
	}
}