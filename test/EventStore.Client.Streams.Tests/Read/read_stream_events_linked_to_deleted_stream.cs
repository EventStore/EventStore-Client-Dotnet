using System.Text;

namespace EventStore.Client.Streams.Tests.Read;

[Trait("Category", "Target:Stream")]
public abstract class read_stream_events_linked_to_deleted_stream(ReadEventsLinkedToDeletedStreamFixture fixture) {
	ReadEventsLinkedToDeletedStreamFixture Fixture { get; } = fixture;

	[Fact]
	public void one_event_is_read() => Assert.Single(Fixture.Events ?? Array.Empty<ResolvedEvent>());

	[Fact]
	public void the_linked_event_is_not_resolved() => Assert.Null(Fixture.Events![0].Event);

	[Fact]
	public void the_link_event_is_included() => Assert.NotNull(Fixture.Events![0].OriginalEvent);

	[Fact]
	public void the_event_is_not_resolved() => Assert.False(Fixture.Events![0].IsResolved);
	
	[Trait("Category", "Operation:Read")]
	[Trait("Category", "Operation:Read:Forwards")]
	public class @forwards(forwards.CustomFixture fixture)
		: read_stream_events_linked_to_deleted_stream(fixture), IClassFixture<forwards.CustomFixture> {
		
		public class CustomFixture() : ReadEventsLinkedToDeletedStreamFixture(Direction.Forwards);
	}
	
	[Trait("Category", "Operation:Read")]
	[Trait("Category", "Operation:Read:Backwards")]
	public class @backwards(backwards.CustomFixture fixture)
		: read_stream_events_linked_to_deleted_stream(fixture), IClassFixture<backwards.CustomFixture> {
		
		public class CustomFixture() : ReadEventsLinkedToDeletedStreamFixture(Direction.Backwards);
	}
}

public abstract class ReadEventsLinkedToDeletedStreamFixture : EventStoreFixture {
	const string DeletedStream = nameof(DeletedStream);
	const string LinkedStream  = nameof(LinkedStream);

	protected ReadEventsLinkedToDeletedStreamFixture(Direction direction) {
		OnSetup = async () => {
			await Streams.AppendToStreamAsync(DeletedStream, StreamState.Any, CreateTestEvents());
				
			await Streams.AppendToStreamAsync(
				LinkedStream,
				StreamState.Any,
				new[] {
					new EventData(
						Uuid.NewUuid(),
						SystemEventTypes.LinkTo,
						Encoding.UTF8.GetBytes($"0@{DeletedStream}"),
						Array.Empty<byte>(),
						Constants.Metadata.ContentTypes.ApplicationOctetStream
					)
				}
			);

			await Streams.DeleteAsync(DeletedStream, StreamState.Any);
			
			Events = await Streams.ReadStreamAsync(
				direction,
				LinkedStream,
				StreamPosition.Start,
				1,
				true
			).ToArrayAsync();
		};
	}

	public ResolvedEvent[]? Events { get; private set; }
}