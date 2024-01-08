namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_existing_with_start_from_beginning_and_no_stream
	: IClassFixture<connect_to_existing_with_start_from_beginning_and_no_stream.Fixture> {
	const string Group = "startinbeginning1";

	const string Stream =
		nameof(connect_to_existing_with_start_from_beginning_and_no_stream);

	readonly Fixture _fixture;

	public connect_to_existing_with_start_from_beginning_and_no_stream(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_subscription_gets_event_zero_as_its_first_event() {
		var firstEvent = await _fixture.FirstEvent.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(StreamPosition.Start, firstEvent.Event.EventNumber);
		Assert.Equal(_fixture.EventId, firstEvent.Event.EventId);
	}

	public class Fixture : EventStoreClientFixture {
		readonly        TaskCompletionSource<ResolvedEvent> _firstEventSource;
		public readonly EventData[]                         Events;
		PersistentSubscription?                             _subscription;

		public Fixture() {
			_firstEventSource = new();
			Events            = CreateTestEvents().ToArray();
		}

		public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;
		public Uuid                EventId    => Events.Single().EventId;

		protected override async Task Given() {
			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);

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
		}

		protected override Task When() => StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}