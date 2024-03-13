namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class happy_case_writing_and_subscribing_to_normal_events_manual_ack
	: IClassFixture<happy_case_writing_and_subscribing_to_normal_events_manual_ack.Fixture> {
	private const string Group = nameof(Group);
	private const int BufferCount = 10;
	private const int EventWriteCount = BufferCount * 2;

	private readonly Fixture _fixture;

	public happy_case_writing_and_subscribing_to_normal_events_manual_ack(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task Test() {
		await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.SelectAwait(async e => {
				await _fixture.Subscription.Ack(e.ResolvedEvent);
				return e;
			})
			.Where(e => e.ResolvedEvent.OriginalStreamId.StartsWith("test-"))
			.Take(_fixture.Events.Length)
			.ToArrayAsync()
			.AsTask()
			.WithTimeout();
	}

	public class Fixture : EventStoreClientFixture {
		public readonly EventData[] Events;

		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public Fixture() {
			Events = CreateTestEvents(EventWriteCount).ToArray();
		}

		protected override async Task Given() {
			await Client.CreateToAllAsync(Group, new(startFrom: Position.End, resolveLinkTos: true),
				userCredentials: TestCredentials.Root);

			Subscription = Client.SubscribeToAll(Group, bufferSize: BufferCount, userCredentials: TestCredentials.Root);
		}

		protected override async Task When() {
			foreach (var e in Events) {
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
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
