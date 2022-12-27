using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class
		connect_to_existing_with_start_from_x_set_higher_than_x_and_events_in_it_then_event_written
		: IClassFixture<
			connect_to_existing_with_start_from_x_set_higher_than_x_and_events_in_it_then_event_written
			.Fixture> {
		private const string Group = "startinbeginning1";

		private const string Stream =
			nameof(
				connect_to_existing_with_start_from_x_set_higher_than_x_and_events_in_it_then_event_written
			);

		private readonly Fixture _fixture;

		public
			connect_to_existing_with_start_from_x_set_higher_than_x_and_events_in_it_then_event_written(
				Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_gets_the_written_event_as_its_first_event() {
			var resolvedEvent = await _fixture.FirstEvent.WithTimeout();
			Assert.Equal(new StreamPosition(11), resolvedEvent.Event.EventNumber);
			Assert.Equal(_fixture.Events.Last().EventId, resolvedEvent.Event.EventId);
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;
			public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;
			public readonly EventData[] Events;
			private PersistentSubscription? _subscription;

			public Fixture() {
				_firstEventSource = new TaskCompletionSource<ResolvedEvent>();
				Events = CreateTestEvents(12).ToArray();
			}

			protected override async Task Given() {
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events.Take(11));
				await Client.CreateToStreamAsync(Stream, Group,
					new PersistentSubscriptionSettings(startFrom: new StreamPosition(11)), userCredentials: TestCredentials.Root);
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
				StreamsClient.AppendToStreamAsync(Stream, new StreamRevision(10), Events.Skip(11));

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
