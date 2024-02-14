namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_existing_with_start_from_set_to_invalid_middle_position
	: IClassFixture<connect_to_existing_with_start_from_set_to_invalid_middle_position.Fixture> {
	private const string Group = "startfrominvalid1";
	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_set_to_invalid_middle_position(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_is_dropped() {
		var ex = await Assert.ThrowsAsync<PersistentSubscriptionDroppedByServerException>(async () =>
			await _fixture.Enumerator!.MoveNextAsync());

#if NET
		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(Group, ex.GroupName);
#endif
	}

	public class Fixture : EventStoreClientFixture {
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }
		public IAsyncEnumerator<PersistentSubscriptionMessage>? Enumerator { get; private set; }

		protected override async Task Given() {
			var invalidPosition = new Position(1L, 1L);
			await Client.CreateToAllAsync(Group, new(startFrom: invalidPosition),
				userCredentials: TestCredentials.Root);
		}

		protected override async Task When() {
			Subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);
			Enumerator = Subscription.Messages.GetAsyncEnumerator();

			await Enumerator.MoveNextAsync();
		}

		public override async Task DisposeAsync() {
			if (Enumerator is not null) {
				await Enumerator.DisposeAsync();
			}

			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
