using System.Text;
using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

// ReSharper disable ClassNeverInstantiated.Global

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Stream")]
public abstract class ReadStreamEventsLinkedToDeletedStreamTests(ReadEventsLinkedToDeletedStreamFixture fixture) {
	ReadEventsLinkedToDeletedStreamFixture Fixture { get; } = fixture;

	[Fact]
	public void one_event_is_read() => Assert.Single(Fixture.Events ?? []);

	[Fact]
	public void the_linked_event_is_not_resolved() => Assert.Null(Fixture.Events![0].Event);

	[Fact]
	public void the_link_event_is_included() => Assert.NotNull(Fixture.Events![0].OriginalEvent);

	[Fact]
	public void the_event_is_not_resolved() => Assert.False(Fixture.Events![0].IsResolved);

	[UsedImplicitly]
	[Trait("Category", "Operation:Read")]
	[Trait("Category", "Operation:Read:Forwards")]
	public class Forwards(Forwards.CustomFixture fixture) : ReadStreamEventsLinkedToDeletedStreamTests(fixture), IClassFixture<Forwards.CustomFixture> {
		public class CustomFixture() : ReadEventsLinkedToDeletedStreamFixture(Direction.Forwards);
	}

	[UsedImplicitly]
	[Trait("Category", "Operation:Read")]
	[Trait("Category", "Operation:Read:Backwards")]
	public class Backwards(Backwards.CustomFixture fixture) : ReadStreamEventsLinkedToDeletedStreamTests(fixture), IClassFixture<Backwards.CustomFixture> {
		public class CustomFixture() : ReadEventsLinkedToDeletedStreamFixture(Direction.Backwards);
	}
}

public abstract class ReadEventsLinkedToDeletedStreamFixture : KurrentTemporaryFixture {
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
