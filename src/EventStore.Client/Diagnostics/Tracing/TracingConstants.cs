#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace EventStore.Client.Diagnostics.Tracing;

public static class TracingConstants {
	public static class Metadata {
		public const string TraceId      = "trace-id";
		public const string SpanId       = "span-id";
		public const string ParentSpanId = "parent-span-id";
	}

	public static class Operations {
		public const string Append = "streams.append";
	}
}
