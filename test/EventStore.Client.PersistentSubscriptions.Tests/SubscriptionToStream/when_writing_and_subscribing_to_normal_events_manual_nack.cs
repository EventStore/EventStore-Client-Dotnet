namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

[Obsolete]
public class when_writing_and_subscribing_to_normal_events_manual_nack
	: IClassFixture<when_writing_and_subscribing_to_normal_events_manual_nack.Fixture> {
	private const string Stream = nameof(when_writing_and_subscribing_to_normal_events_manual_nack);
	private const string Group = nameof(Group);
	private const int BufferCount = 10;
	private const int EventWriteCount = BufferCount * 2;

	private readonly Fixture _fixture;

	public when_writing_and_subscribing_to_normal_events_manual_nack(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task Test() {
		await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(1)
			.ForEachAwaitAsync(async message =>
				await _fixture.Subscription.Nack(PersistentSubscriptionNakEventAction.Park, "fail",
					message.ResolvedEvent))
			.WithTimeout();
	}

	public class Fixture : EventStoreClientFixture {
		private readonly EventData[] _events;
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public Fixture() {
			_events = CreateTestEvents(EventWriteCount).ToArray();
		}

		protected override async Task Given() {
			await Client.CreateToStreamAsync(Stream, Group, new(startFrom: StreamPosition.Start, resolveLinkTos: true),
				userCredentials: TestCredentials.Root);

			Subscription = Client.SubscribeToStream(Stream, Group, bufferSize: BufferCount, userCredentials: TestCredentials.Root);
		}

		protected override async Task When() {
			foreach (var e in _events) {
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, new[] { e });
			}
		}

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
