#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace EventStore.Client.Diagnostics.Tracing;

public record TracingMetadata(string? TraceId, string? SpanId, string? ParentSpanId) { }
