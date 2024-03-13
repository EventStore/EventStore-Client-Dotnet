namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_existing_with_start_from_set_to_end_position
	: IClassFixture<connect_to_existing_with_start_from_set_to_end_position.Fixture> {
	private const string Group = "startfromend1";

	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_set_to_end_position(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_gets_no_non_system_events() {
		await Assert.ThrowsAsync<TimeoutException>(() => _fixture.Subscription!.Messages
			.OfType<PersistentSubscriptionMessage.Event>()
			.Where(e => !SystemStreams.IsSystemStream(e.ResolvedEvent.OriginalStreamId))
			.AnyAsync()
			.AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(250)));
	}

	public class Fixture : EventStoreClientFixture {
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		protected override async Task Given() {
			foreach (var @event in CreateTestEvents(10)) {
				await StreamsClient.AppendToStreamAsync("non-system-stream-" + Guid.NewGuid(), StreamState.Any,
					new[] { @event });
			}

			await Client.CreateToAllAsync(Group, new(startFrom: Position.End), userCredentials: TestCredentials.Root);
		}

		protected override Task When() {
			Subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);
			return Task.CompletedTask;
		}

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
