using System;
using System.Collections.Generic;

#nullable enable
namespace EventStore.Client {
	public class EventRecord {
		public readonly string EventStreamId;
		public readonly Uuid EventId;
		public readonly StreamPosition EventNumber;
		public readonly string EventType;
		public readonly ReadOnlyMemory<byte> Data;
		public readonly ReadOnlyMemory<byte> Metadata;
		public readonly DateTime Created;
		public readonly Position Position;
		public readonly string ContentType;

		public EventRecord(
			string eventStreamId,
			Uuid eventId,
			StreamPosition eventNumber,
			Position position,
			IDictionary<string, string> metadata,
			ReadOnlyMemory<byte> data,
			ReadOnlyMemory<byte> customMetadata) {
			EventStreamId = eventStreamId;
			EventId = eventId;
			EventNumber = eventNumber;
			Position = position;
			Data = data;
			Metadata = customMetadata;
			EventType = metadata[Constants.Metadata.Type];
			Created = Convert.ToInt64(metadata[Constants.Metadata.Created]).FromTicksSinceEpoch();
			ContentType = metadata[Constants.Metadata.ContentType];
		}
	}
}
