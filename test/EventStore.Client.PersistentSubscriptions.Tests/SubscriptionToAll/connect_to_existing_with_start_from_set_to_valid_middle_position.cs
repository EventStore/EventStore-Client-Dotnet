using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_existing_with_start_from_set_to_valid_middle_position
		: IClassFixture<connect_to_existing_with_start_from_set_to_valid_middle_position.
			Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "startfromvalid";



		public connect_to_existing_with_start_from_set_to_valid_middle_position(
			Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_gets_the_event_at_the_specified_start_position_as_its_first_event() {
			var resolvedEvent = await _fixture.FirstEvent.WithTimeout();
			Assert.Equal(_fixture.ExpectedEvent.OriginalPosition, resolvedEvent.Event.Position);
			Assert.Equal(_fixture.ExpectedEvent.Event.EventId, resolvedEvent.Event.EventId);
			Assert.Equal(_fixture.ExpectedEvent.Event.EventStreamId, resolvedEvent.Event.EventStreamId);
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;
			public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;
			private PersistentSubscription _subscription;
			public ResolvedEvent ExpectedEvent { get; private set; }

			public Fixture() {
				_firstEventSource = new TaskCompletionSource<ResolvedEvent>();
			}

			protected override async Task Given() {
				var events = await StreamsClient.ReadAllAsync(Direction.Forwards, Position.Start, 10,
					userCredentials: TestCredentials.Root).ToArrayAsync();
				ExpectedEvent = events[events.Length / 2]; //just a random event in the middle of the results

				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(startFrom: ExpectedEvent.OriginalPosition), TestCredentials.Root);
			}

			protected override async Task When() {
				_subscription = await Client.SubscribeToAllAsync(Group,
					async (subscription, e, r, ct) => {
						_firstEventSource.TrySetResult(e);
						await subscription.Ack(e);
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
