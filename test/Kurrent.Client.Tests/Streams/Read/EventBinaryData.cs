using EventStore.Client;

namespace Kurrent.Client.Tests;

public readonly record struct EventBinaryData(Uuid Id, byte[] Data, byte[] Metadata) {
	public bool Equals(EventBinaryData other) =>
		Id.Equals(other.Id)
	 && Data.SequenceEqual(other.Data)
	 && Metadata.SequenceEqual(other.Metadata);

	public override int GetHashCode() => System.HashCode.Combine(Id, Data, Metadata);
}

public static class EventBinaryDataConverters {
	public static EventBinaryData ToBinaryData(this EventData source) =>
		new(source.EventId, source.Data.ToArray(), source.Metadata.ToArray());

	public static EventBinaryData ToBinaryData(this EventRecord source) =>
		new(source.EventId, source.Data.ToArray(), source.Metadata.ToArray());

	public static EventBinaryData ToBinaryData(this ResolvedEvent source) =>
		source.Event.ToBinaryData();

	public static EventBinaryData[] ToBinaryData(this IEnumerable<EventData> source) =>
		source.Select(x => x.ToBinaryData()).ToArray();

	public static EventBinaryData[] ToBinaryData(this IEnumerable<EventRecord> source) =>
		source.Select(x => x.ToBinaryData()).ToArray();

	public static EventBinaryData[] ToBinaryData(this IEnumerable<ResolvedEvent> source) =>
		source.Select(x => x.ToBinaryData()).ToArray();

	public static ValueTask<EventBinaryData[]> ToBinaryData(this IAsyncEnumerable<ResolvedEvent> source) =>
		source.DefaultIfEmpty().Select(x => x.ToBinaryData()).ToArrayAsync();
}
