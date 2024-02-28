namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class update_existing_with_check_point
	: IClassFixture<update_existing_with_check_point.Fixture> {
	private const string Stream = nameof(update_existing_with_check_point);
	private const string Group = "existing-with-check-point";
	private readonly Fixture _fixture;

	public update_existing_with_check_point(Fixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task resumes_from_check_point() {
		await using var subscription =
			_fixture.Client.SubscribeToStream(Stream, Group, userCredentials: TestCredentials.Root);

		var resolvedEvent = await subscription.Messages
			.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstAsync()
			.AsTask()
			.WithTimeout();

		Assert.Equal(_fixture.CheckPoint.Next(), resolvedEvent.Event.EventNumber);
	}

	public class Fixture : EventStoreClientFixture {
		private readonly EventData[] _events;

		public Fixture() {
			_events = CreateTestEvents(5).ToArray();
		}

		public StreamPosition CheckPoint { get; private set; }

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, _events);

			await Client.CreateToStreamAsync(Stream, Group,
				new(checkPointLowerBound: 5, checkPointAfter: TimeSpan.FromSeconds(1), startFrom: StreamPosition.Start),
				userCredentials: TestCredentials.Root);

			await using var subscription =
				Client.SubscribeToStream(Stream, Group, userCredentials: TestCredentials.Root);

			await using var enumerator = subscription.Messages.GetAsyncEnumerator();

			await enumerator.MoveNextAsync();

			await Task.WhenAll(Subscribe().WithTimeout(), WaitForCheckpoint().WithTimeout());

			return;

			async Task Subscribe() {
				var count = 0;

				while (await enumerator.MoveNextAsync()) {
					if (enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
						continue;
					}

					count++;

					await subscription.Ack(resolvedEvent);
					if (count >= _events.Length) {
						break;
					}
				}
			}

			async Task WaitForCheckpoint() {
				await using var subscription = StreamsClient.SubscribeToStream(
					$"$persistentsubscription-{Stream}::{Group}-checkpoint", FromStream.Start,
					userCredentials: TestCredentials.Root);

				await foreach (var message in subscription.Messages) {
					if (message is not StreamMessage.Event (var resolvedEvent)) {
						continue;
					}

					CheckPoint = resolvedEvent.Event.Data.ParseStreamPosition();
					return;
				}
			}
		}

		protected override async Task When() {
			// Force restart of the subscription
			await Client.UpdateToStreamAsync(Stream, Group, new(), userCredentials: TestCredentials.Root);

			await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, CreateTestEvents(1));
		}
	}
}
