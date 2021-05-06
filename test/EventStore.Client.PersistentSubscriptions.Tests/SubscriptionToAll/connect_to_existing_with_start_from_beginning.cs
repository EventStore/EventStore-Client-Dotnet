using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_existing_with_start_from_beginning
		: IClassFixture<connect_to_existing_with_start_from_beginning.Fixture
		> {
		private readonly Fixture _fixture;

		private const string Group = "startfrombeginning";



		public connect_to_existing_with_start_from_beginning(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_gets_event_zero_as_its_first_event() {
			var resolvedEvent = await _fixture.FirstEvent.WithTimeout(TimeSpan.FromSeconds(10));
			Assert.Equal(_fixture.Events[0].Event.EventId, resolvedEvent.Event.EventId);
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;
			public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;
			public ResolvedEvent[] Events;
			private PersistentSubscription _subscription;

			public Fixture() {
				_firstEventSource = new TaskCompletionSource<ResolvedEvent>();
			}

			protected override async Task Given() {
				//append 10 events to random streams to make sure we have at least 10 events in the transaction file
				foreach (var @event in CreateTestEvents(10)) {
					await StreamsClient.AppendToStreamAsync(Guid.NewGuid().ToString(), StreamState.NoStream, new []{ @event });
				}
				Events = await StreamsClient.ReadAllAsync(Direction.Forwards, Position.Start, 10, userCredentials: TestCredentials.Root).ToArrayAsync();

				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(startFrom: Position.Start), TestCredentials.Root);
			}

			protected override async Task When() {
				_subscription = await Client.SubscribeToAllAsync(Group,
					(subscription, e, r, ct) => {
						_firstEventSource.TrySetResult(e);
						return Task.CompletedTask;
					}, (subscription, reason, ex) => {
						if (reason != SubscriptionDroppedReason.Disposed) {
							_firstEventSource.TrySetException(ex!);
						}
					}, TestCredentials.Root);
			}

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
