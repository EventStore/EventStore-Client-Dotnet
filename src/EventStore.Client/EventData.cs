using System;
using System.Net.Http.Headers;

namespace EventStore.Client {
	/// <summary>
	/// Represents an event to be written.
	/// </summary>
	public sealed class EventData {
		/// <summary>
		/// The raw bytes of the event data.
		/// </summary>
		public readonly ReadOnlyMemory<byte> Data;

		/// <summary>
		/// The <see cref="Uuid"/> of the event, used as part of the idempotent write check.
		/// </summary>
		public readonly Uuid EventId;

		/// <summary>
		/// The raw bytes of the event metadata.
		/// </summary>
		public readonly ReadOnlyMemory<byte> Metadata;

		/// <summary>
		/// The name of the event type. It is strongly recommended that these
		/// use lowerCamelCase if projections are to be used.
		/// </summary>
		public readonly string Type;

		/// <summary>
		/// The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.
		/// </summary>
		public readonly string ContentType;

		/// <summary>
		/// Constructs a new <see cref="EventData"/>.
		/// </summary>
		/// <param name="eventId">The <see cref="Uuid"/> of the event, used as part of the idempotent write check.</param>
		/// <param name="type">The name of the event type. It is strongly recommended that these use lowerCamelCase if projections are to be used.</param>
		/// <param name="data">The raw bytes of the event data.</param>
		/// <param name="metadata">The raw bytes of the event metadata.</param>
		/// <param name="contentType">The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public EventData(Uuid eventId, string type, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte>? metadata = null,
			string contentType = Constants.Metadata.ContentTypes.ApplicationJson) {
			if (eventId == Uuid.Empty) {
				throw new ArgumentOutOfRangeException(nameof(eventId));
			}

			MediaTypeHeaderValue.Parse(contentType);

			if (contentType != Constants.Metadata.ContentTypes.ApplicationJson &&
			    contentType != Constants.Metadata.ContentTypes.ApplicationOctetStream) {
				throw new ArgumentOutOfRangeException(nameof(contentType), contentType,
					$"Only {Constants.Metadata.ContentTypes.ApplicationJson} or {Constants.Metadata.ContentTypes.ApplicationOctetStream} are acceptable values.");
			}

			EventId = eventId;
			Type = type;
			Data = data;
			Metadata = metadata ?? Array.Empty<byte>();
			ContentType = contentType;
		}
	}
}
