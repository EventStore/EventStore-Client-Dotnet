using System;
using System.Collections.Generic;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Represents a previously written event.
	/// </summary>
	public class EventRecord {
		/// <summary>
		/// The stream that this event belongs to.
		/// </summary>
		public readonly string EventStreamId;

		/// <summary>
		/// The <see cref="Uuid"/> representing this event.
		/// </summary>
		public readonly Uuid EventId;

		/// <summary>
		/// The <see cref="StreamPosition"/> of this event in the stream.
		/// </summary>
		public readonly StreamPosition EventNumber;

		/// <summary>
		/// The type of event this is.
		/// </summary>
		public readonly string EventType;

		/// <summary>
		/// The raw bytes representing the data of this event.
		/// </summary>
		public readonly ReadOnlyMemory<byte> Data;

		/// <summary>
		/// The raw bytes representing the metadata of this event.
		/// </summary>
		public readonly ReadOnlyMemory<byte> Metadata;

		/// <summary>
		/// A UTC <see cref="System.DateTime"/> representing when this event was created in the system.
		/// </summary>
		public readonly DateTime Created;

		/// <summary>
		/// The <see cref="Position"/>  of this event in the $all stream.
		/// </summary>
		public readonly Position Position;

		/// <summary>
		/// The Content-Type of the event's data.
		/// </summary>
		public readonly string ContentType;

		/// <summary>
		/// Constructs a new <see cref="EventRecord"/>.
		/// </summary>
		/// <param name="eventStreamId"></param>
		/// <param name="eventId"></param>
		/// <param name="eventNumber"></param>
		/// <param name="position"></param>
		/// <param name="metadata"></param>
		/// <param name="data"></param>
		/// <param name="customMetadata"></param>
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
