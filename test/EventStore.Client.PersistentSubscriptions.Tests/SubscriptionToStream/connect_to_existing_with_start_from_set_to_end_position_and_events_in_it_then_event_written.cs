using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class
		connect_to_existing_with_start_from_set_to_end_position_and_events_in_it_then_event_written
		: IClassFixture<
			connect_to_existing_with_start_from_set_to_end_position_and_events_in_it_then_event_written
			.Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "startinbeginning1";

		private const string Stream =
			nameof(
				connect_to_existing_with_start_from_set_to_end_position_and_events_in_it_then_event_written
			);

		public
			connect_to_existing_with_start_from_set_to_end_position_and_events_in_it_then_event_written(
				Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_gets_the_written_event_as_its_first_event() {
			var resolvedEvent = await _fixture.FirstEvent.WithTimeout();
			Assert.Equal(new StreamPosition(10), resolvedEvent.Event.EventNumber);
			Assert.Equal(_fixture.Events.Last().EventId, resolvedEvent.Event.EventId);
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;
			public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;
			public readonly EventData[] Events;
			private PersistentSubscription? _subscription;

			public Fixture() {
				_firstEventSource = new TaskCompletionSource<ResolvedEvent>();
				Events = CreateTestEvents(11).ToArray();
			}

			protected override async Task Given() {
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events.Take(10));
				await Client.CreateAsync(Stream, Group,
					new PersistentSubscriptionSettings(startFrom: StreamPosition.End), userCredentials: TestCredentials.Root);
				_subscription = await Client.SubscribeToStreamAsync(Stream, Group,
					async (subscription, e, r, ct) => {
						_firstEventSource.TrySetResult(e);
						await subscription.Ack(e);
					}, (subscription, reason, ex) => {
						if (reason != SubscriptionDroppedReason.Disposed) {
							_firstEventSource.TrySetException(ex!);
						}
					}, TestCredentials.TestUser1);
			}

			protected override Task When() =>
				StreamsClient.AppendToStreamAsync(Stream, new StreamRevision(9), Events.Skip(10));

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
