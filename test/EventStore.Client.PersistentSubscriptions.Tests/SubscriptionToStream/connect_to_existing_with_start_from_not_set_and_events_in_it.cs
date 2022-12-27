using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class connect_to_existing_with_start_from_not_set_and_events_in_it
		: IClassFixture<connect_to_existing_with_start_from_not_set_and_events_in_it.
			Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "startinbeginning1";

		private const string Stream =
			nameof(connect_to_existing_with_start_from_not_set_and_events_in_it);

		public connect_to_existing_with_start_from_not_set_and_events_in_it(
			Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_gets_no_events() {
			await Assert.ThrowsAsync<TimeoutException>(() => _fixture.FirstEvent.WithTimeout());
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;
			public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;
			public readonly EventData[] Events;
			private PersistentSubscription? _subscription;

			public Fixture() {
				_firstEventSource = new TaskCompletionSource<ResolvedEvent>();
				Events = CreateTestEvents(10).ToArray();
			}

			protected override async Task Given() {
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);
				await Client.CreateToStreamAsync(Stream, Group,
					new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
			}

			protected override async Task When() {
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

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
