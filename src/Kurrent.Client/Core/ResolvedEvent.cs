using System.Diagnostics.CodeAnalysis;
using EventStore.Client.Serialization;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client {
	/// <summary>
	/// A structure representing a single event or a resolved link event.
	/// </summary>
	public readonly struct ResolvedEvent {
		/// <summary>
		/// If this <see cref="ResolvedEvent"/> represents a link event, the <see cref="Event"/>
		/// will be the resolved link event, otherwise it will be the single event.
		/// </summary>
		public readonly EventRecord Event;

		/// <summary>
		/// The link event if this <see cref="ResolvedEvent"/> is a link event.
		/// </summary>
		public readonly EventRecord? Link;

		/// <summary>
		/// Returns the event that was read or which triggered the subscription.
		///
		/// If this <see cref="ResolvedEvent"/> represents a link event, the <see cref="OriginalEvent"/>
		/// will be the <see cref="Link"/>, otherwise it will be <see cref="Event"/>.
		/// </summary>
		public EventRecord OriginalEvent => Link ?? Event;

		/// <summary>
		/// Returns the deserialized event payload.
		/// It will be provided or equal to null, depending on the automatic deserialization settings you choose.
		/// If it's null, you can use  <see cref="OriginalEvent"/> to deserialize it manually. 
		/// </summary>
		public readonly object? Message;

		/// <summary>
		/// Position of the <see cref="OriginalEvent"/> if available.
		/// </summary>
		public readonly Position? OriginalPosition;

		/// <summary>
		/// The stream name of the <see cref="OriginalEvent" />.
		/// </summary>
		public string OriginalStreamId => OriginalEvent.EventStreamId;

		/// <summary>
		/// The <see cref="StreamPosition"/> in the stream of the <see cref="OriginalEvent"/>.
		/// </summary>
		public StreamPosition OriginalEventNumber => OriginalEvent.EventNumber;

		/// <summary>
		/// Indicates whether this <see cref="ResolvedEvent"/> is a resolved link
		/// event.
		/// </summary>
		public bool IsResolved => Link != null && Event != null;

		/// <summary>
		/// Constructs a new <see cref="ResolvedEvent"/>.
		/// </summary>
		/// <param name="event"></param>
		/// <param name="link"></param>
		/// <param name="commitPosition"></param>
		/// <param name="deserializedEvent"></param>
		public ResolvedEvent(EventRecord @event, EventRecord? link, ulong? commitPosition) : this(
			@event,
			link,
			null,
			commitPosition
		) { }

		/// <summary>
		/// Constructs a new <see cref="ResolvedEvent"/>.
		/// </summary>
		/// <param name="event"></param>
		/// <param name="link"></param>
		/// <param name="message"></param>
		/// <param name="commitPosition"></param>
		ResolvedEvent(
			EventRecord @event,
			EventRecord? link,
			object? message,
			ulong? commitPosition
		) {
			Event   = @event;
			Link    = link;
			Message = message;
			OriginalPosition = commitPosition.HasValue
				? new Position(commitPosition.Value, (link ?? @event).Position.PreparePosition)
				: new Position?();
		}

		public static ResolvedEvent From(
			EventRecord @event,
			EventRecord? link,
			ulong? commitPosition,
			IMessageSerializer messageSerializer
		) {
			var originalEvent = link ?? @event;
			return messageSerializer.TryDeserialize(originalEvent, out var deserialized)
				? new ResolvedEvent(@event, link, deserialized, commitPosition)
				: new ResolvedEvent(@event, link, commitPosition);
		}
	}
}
