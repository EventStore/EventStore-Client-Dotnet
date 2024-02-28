namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class when_writing_and_filtering_out_events : IClassFixture<when_writing_and_filtering_out_events.Fixture> {
	private const string Group = "filtering-out-events";

	private readonly Fixture _fixture;

	public when_writing_and_filtering_out_events(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public void it_should_write_a_check_point() {
		Assert.True(_fixture.SecondCheckPoint > _fixture.FirstCheckPoint);
		Assert.Equal(_fixture.Events.Select(e => e.EventId), _fixture.AppearedEvents.Select(e => e.Event.EventId));
	}

	public class Fixture : EventStoreClientFixture {
		private readonly List<ResolvedEvent> _appearedEvents;

		public Fixture() {
			Events = CreateTestEvents(10).ToArray();
			_appearedEvents = new();
		}

		public Position SecondCheckPoint { get; private set; }
		public Position FirstCheckPoint { get; private set; }
		public EventData[] Events { get; }
		public ResolvedEvent[] AppearedEvents => _appearedEvents.ToArray();

		protected override async Task Given() {
			foreach (var e in Events) {
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
			}

			await Client.CreateToAllAsync(Group, StreamFilter.Prefix("test"),
				new(checkPointLowerBound: 1, checkPointUpperBound: 5, checkPointAfter: TimeSpan.FromSeconds(1),
					startFrom: Position.Start), userCredentials: TestCredentials.Root);

			await using var subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);
			await using var enumerator = subscription.Messages.GetAsyncEnumerator();

			await Task.WhenAll(Subscribe().WithTimeout(), WaitForCheckpoints().WithTimeout());

			return;

			async Task Subscribe() {
				while (await enumerator.MoveNextAsync()) {
					if (enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
						continue;
					}

					_appearedEvents.Add(resolvedEvent);
					await subscription.Ack(resolvedEvent);
					if (_appearedEvents.Count == Events.Length) {
						break;
					}
				}
			}

			async Task WaitForCheckpoints() {
				bool firstCheckpointSet = false;
				await using var subscription = StreamsClient.SubscribeToStream(
					$"$persistentsubscription-$all::{Group}-checkpoint", FromStream.Start,
					userCredentials: TestCredentials.Root);
				await foreach (var message in subscription.Messages) {
					if (message is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					if (!firstCheckpointSet) {
						FirstCheckPoint = resolvedEvent.Event.Data.ParsePosition();
						firstCheckpointSet = true;
					} else {
						SecondCheckPoint = resolvedEvent.Event.Data.ParsePosition();
						return;
					}
				}
			}
		}

		protected override async Task When() {
			foreach (var e in Events) {
				await StreamsClient.AppendToStreamAsync("filtered-out-stream-" + Guid.NewGuid(), StreamState.Any,
					new[] { e });
			}
		}
	}
}
