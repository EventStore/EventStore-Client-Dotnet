// ReSharper disable CheckNamespace

namespace EventStore.Diagnostics.Telemetry;

static partial class TelemetryTags {
    public static class EventStore {
        public const string Stream         = "db.eventstoredb.stream";
        public const string SubscriptionId = "db.eventstoredb.subscription.id";
        public const string EventId        = "db.eventstoredb.event.id";
        public const string EventType      = "db.eventstoredb.event.type";
    }
}