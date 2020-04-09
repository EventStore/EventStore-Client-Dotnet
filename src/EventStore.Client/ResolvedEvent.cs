#nullable enable
namespace EventStore.Client {
	public readonly struct ResolvedEvent {
		public readonly EventRecord Event;
		public readonly EventRecord? Link;

		public EventRecord OriginalEvent => Link ?? Event;

		/// <summary>
		/// Position of the OriginalEvent (unresolved link or event) if available
		/// </summary>
		public readonly Position? OriginalPosition;

		public string OriginalStreamId => OriginalEvent.EventStreamId;

		public StreamRevision OriginalEventNumber => OriginalEvent.EventNumber;

		public bool IsResolved => Link != null && Event != null;

		public ResolvedEvent(EventRecord @event, EventRecord? link, ulong? commitPosition) {
			Event = @event;
			Link = link;
			OriginalPosition = commitPosition.HasValue
				? new Position(commitPosition.Value, (link ?? @event).Position.PreparePosition)
				: new Position?();
		}
	}
}
