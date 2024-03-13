using System.Text;

namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

[Obsolete]
public class happy_case_catching_up_to_link_to_events_manual_ack_obsolete
	: IClassFixture<happy_case_catching_up_to_link_to_events_manual_ack_obsolete.Fixture> {
	const string Stream          = nameof(happy_case_catching_up_to_link_to_events_manual_ack_obsolete);
	const string Group           = nameof(Group);
	const int    BufferCount     = 10;
	const int    EventWriteCount = BufferCount * 2;

	readonly Fixture _fixture;

	public happy_case_catching_up_to_link_to_events_manual_ack_obsolete(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task Test() => await _fixture.EventsReceived.WithTimeout();

	public class Fixture : EventStoreClientFixture {
		readonly EventData[]                _events;
		readonly TaskCompletionSource<bool> _eventsReceived;
		int                                 _eventReceivedCount;

		PersistentSubscription? _subscription;

		public Fixture() {
			_events = CreateTestEvents(EventWriteCount)
				.Select(
					(e, i) => new EventData(
						e.EventId,
						SystemEventTypes.LinkTo,
						Encoding.UTF8.GetBytes($"{i}@{Stream}"),
						contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
					)
				)
				.ToArray();

			_eventsReceived = new();
		}

		public Task EventsReceived => _eventsReceived.Task;

		protected override async Task Given() {
			foreach (var e in _events)
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, new[] { e });

			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(startFrom: StreamPosition.Start, resolveLinkTos: true),
				userCredentials: TestCredentials.Root
			);

			_subscription = await Client.SubscribeToStreamAsync(
				Stream,
				Group,
				async (subscription, e, retryCount, ct) => {
					await subscription.Ack(e);

					if (Interlocked.Increment(ref _eventReceivedCount) == _events.Length)
						_eventsReceived.TrySetResult(true);
				},
				(s, r, e) => {
					if (e != null)
						_eventsReceived.TrySetException(e);
				},
				bufferSize: BufferCount,
				userCredentials: TestCredentials.Root
			);
		}

		protected override Task When() => Task.CompletedTask;

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
