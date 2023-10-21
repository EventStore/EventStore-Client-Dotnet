using System.Text;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class read_all_events_forward_with_linkto_passed_max_count
		: IClassFixture<read_all_events_forward_with_linkto_passed_max_count.Fixture> {
		private readonly Fixture _fixture;

		public read_all_events_forward_with_linkto_passed_max_count(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public void one_event_is_read() {
			Assert.Single(_fixture.Events ?? Array.Empty<ResolvedEvent>());
		}

		public class Fixture : EventStoreClientFixture {
			private const string DeletedStream = nameof(DeletedStream);
			private const string LinkedStream = nameof(LinkedStream);
			public ResolvedEvent[]? Events { get; private set; }

			protected override async Task Given() {
				await Client.AppendToStreamAsync(DeletedStream, StreamState.Any, CreateTestEvents());
				await Client.SetStreamMetadataAsync(DeletedStream, StreamState.Any,
					new StreamMetadata(maxCount: 2));
				await Client.AppendToStreamAsync(DeletedStream, StreamState.Any, CreateTestEvents());
				await Client.AppendToStreamAsync(DeletedStream, StreamState.Any, CreateTestEvents());
				await Client.AppendToStreamAsync(LinkedStream, StreamState.Any, new[] {
					new EventData(
						Uuid.NewUuid(), SystemEventTypes.LinkTo, Encoding.UTF8.GetBytes("0@" + DeletedStream),
						Array.Empty<byte>(), Constants.Metadata.ContentTypes.ApplicationOctetStream)
				});
			}

			protected override async Task When() {
				Events = await Client.ReadStreamAsync(Direction.Forwards, LinkedStream, StreamPosition.Start,
						resolveLinkTos: true)
					.ToArrayAsync();
			}
		}
	}
}
