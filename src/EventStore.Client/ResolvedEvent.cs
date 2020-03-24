#nullable enable
namespace EventStore.Client {
	public readonly struct ResolvedEvent {
		public readonly EventRecord Event;
		public readonly EventRecord? Link;

		public EventRecord OriginalEvent {
			get { return Link ?? Event; }
		}

		/// <summary>
		/// Position of the OriginalEvent (unresolved link or event) if available
		/// </summary>
		public readonly Position? OriginalPosition;

		public string OriginalStreamId => OriginalEvent.EventStreamId;

		public StreamRevision OriginalEventNumber => OriginalEvent.EventNumber;

		public bool IsResolved => Link != null && Event != null;

		public ResolvedEvent(EventRecord @event, EventRecord? link, long? commitPosition) {
			Event = @event;
			Link = link;
			OriginalPosition = commitPosition.HasValue
				? new Position((ulong)commitPosition.Value, (link ?? @event).Position.PreparePosition)
				: default;
		}
	}
}
