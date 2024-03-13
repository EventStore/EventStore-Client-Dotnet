namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class happy_case_catching_up_to_normal_events_manual_ack
	: IClassFixture<happy_case_catching_up_to_normal_events_manual_ack.Fixture> {
	private const string Stream = nameof(happy_case_catching_up_to_normal_events_manual_ack);
	private const string Group = nameof(Group);
	private const int BufferCount = 10;
	private const int EventWriteCount = BufferCount * 2;

	private readonly Fixture _fixture;

	public happy_case_catching_up_to_normal_events_manual_ack(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task Test() {
		await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(_fixture.Events.Length)
			.ForEachAwaitAsync(e => _fixture.Subscription.Ack(e.ResolvedEvent))
			.WithTimeout();
	}

	public class Fixture : EventStoreClientFixture {
		public readonly EventData[] Events;

		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }


		public Fixture() {
			Events = CreateTestEvents(EventWriteCount).ToArray();
		}

		protected override async Task Given() {
			foreach (var e in Events)
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, new[] { e });

			await Client.CreateToStreamAsync(Stream, Group, new(startFrom: StreamPosition.Start, resolveLinkTos: true),
				userCredentials: TestCredentials.Root);

			Subscription = Client.SubscribeToStream(Stream, Group, bufferSize: BufferCount,
				userCredentials: TestCredentials.Root);
		}

		protected override Task When() => Task.CompletedTask;

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
