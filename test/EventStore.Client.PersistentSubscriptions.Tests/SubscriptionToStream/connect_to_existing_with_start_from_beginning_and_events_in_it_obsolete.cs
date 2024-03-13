namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

[Obsolete]
public class connect_to_existing_with_start_from_beginning_and_events_in_it_obsolete
	: IClassFixture<connect_to_existing_with_start_from_beginning_and_events_in_it_obsolete.Fixture
	> {
	const string Group = "startinbeginning1";

	const string Stream =
		nameof(connect_to_existing_with_start_from_beginning_and_events_in_it_obsolete);

	readonly Fixture _fixture;

	public connect_to_existing_with_start_from_beginning_and_events_in_it_obsolete(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_subscription_gets_event_zero_as_its_first_event() {
		var resolvedEvent = await _fixture.FirstEvent.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(StreamPosition.Start, resolvedEvent.Event.EventNumber);
		Assert.Equal(_fixture.Events[0].EventId, resolvedEvent.Event.EventId);
	}

	public class Fixture : EventStoreClientFixture {
		readonly        TaskCompletionSource<ResolvedEvent> _firstEventSource;
		public readonly EventData[]                         Events;
		PersistentSubscription?                             _subscription;

		public Fixture() {
			_firstEventSource = new();
			Events            = CreateTestEvents(10).ToArray();
		}

		public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);
			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(startFrom: StreamPosition.Start),
				userCredentials: TestCredentials.Root
			);
		}

		protected override async Task When() =>
			_subscription = await Client.SubscribeToStreamAsync(
				Stream,
				Group,
				async (subscription, e, r, ct) => {
					_firstEventSource.TrySetResult(e);
					await subscription.Ack(e);
				},
				(subscription, reason, ex) => {
					if (reason != SubscriptionDroppedReason.Disposed)
						_firstEventSource.TrySetException(ex!);
				},
				TestCredentials.TestUser1
			);

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
