namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream.Obsolete;

[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class connect_to_existing_with_start_from_not_set_and_events_in_it_then_event_written_obsolete
	: IClassFixture<connect_to_existing_with_start_from_not_set_and_events_in_it_then_event_written_obsolete.Fixture> {
	const string Group  = "startinbeginning1";
	const string Stream = nameof(connect_to_existing_with_start_from_not_set_and_events_in_it_then_event_written_obsolete);

	readonly Fixture _fixture;

	public
		connect_to_existing_with_start_from_not_set_and_events_in_it_then_event_written_obsolete(Fixture fixture) =>
		_fixture = fixture;

	[Fact]
	public async Task the_subscription_gets_the_written_event_as_its_first_event() {
		var resolvedEvent = await _fixture.FirstEvent.WithTimeout();
		Assert.Equal(new(10), resolvedEvent.Event.EventNumber);
		Assert.Equal(_fixture.Events.Last().EventId, resolvedEvent.Event.EventId);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;

		public readonly EventData[] Events;

		PersistentSubscription? _subscription;

		public Fixture() {
			_firstEventSource = new();
			Events            = CreateTestEvents(11).ToArray();
		}

		public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events.Take(10));
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

		protected override Task When() => StreamsClient.AppendToStreamAsync(Stream, new StreamRevision(9), Events.Skip(10));

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
