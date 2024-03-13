using System.Text;

namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class happy_case_catching_up_to_link_to_events_manual_ack 
	: IClassFixture<happy_case_catching_up_to_link_to_events_manual_ack.Fixture> {
	private const string Group = nameof(Group);
	private const int BufferCount = 10;
	private const int EventWriteCount = BufferCount * 2;

	private readonly Fixture _fixture;

	public happy_case_catching_up_to_link_to_events_manual_ack(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
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
			Events = CreateTestEvents(EventWriteCount)
				.Select((e, i) => new EventData(e.EventId, SystemEventTypes.LinkTo, Encoding.UTF8.GetBytes($"{i}@test"),
					contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream))
				.ToArray();
		}

		protected override async Task Given() {
			foreach (var e in Events) {
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
			}

			await Client.CreateToAllAsync(Group, new(startFrom: Position.Start, resolveLinkTos: true),
				userCredentials: TestCredentials.Root);

			Subscription = Client.SubscribeToAll(Group, bufferSize: BufferCount, userCredentials: TestCredentials.Root);
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
