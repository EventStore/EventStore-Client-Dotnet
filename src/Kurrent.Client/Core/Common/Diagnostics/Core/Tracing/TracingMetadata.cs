// ReSharper disable CheckNamespace

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kurrent.Diagnostics.Tracing;

readonly record struct TracingMetadata(
	[property: JsonPropertyName(TracingConstants.Metadata.TraceId)]
	string? TraceId,
	[property: JsonPropertyName(TracingConstants.Metadata.SpanId)]
	string? SpanId
) {
	public static readonly TracingMetadata None = new(null, null);

	[JsonIgnore] public bool IsValid => TraceId != null && SpanId != null;

	public ActivityContext? ToActivityContext(bool isRemote = true) {
		try {
			return IsValid
				? new ActivityContext(
					ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(TraceId!.ToCharArray())),
					ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(SpanId!.ToCharArray())),
					ActivityTraceFlags.Recorded,
					isRemote: isRemote
				)
				: default;
		} catch (Exception) {
			return default;
		}
	}
}
