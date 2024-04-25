// ReSharper disable CheckNamespace

namespace EventStore.Diagnostics.Tracing;

static partial class TracingConstants {
    public static class Metadata {
        public const string TraceId = "$traceId";
        public const string SpanId  = "$spanId";
    }
}