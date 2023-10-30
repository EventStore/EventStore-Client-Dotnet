namespace EventStore.Client.PersistentSubscriptions.Tests;

public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
	readonly bool _skipPsWarmUp;

	protected EventStoreClientFixture(EventStoreClientSettings? settings = null, bool skipPSWarmUp = false, bool noDefaultCredentials = false)
		: base(settings, noDefaultCredentials: noDefaultCredentials) {
		_skipPsWarmUp = skipPSWarmUp;

		Client               = new(Settings);
		StreamsClient        = new(Settings);
		UserManagementClient = new(Settings);
	}

	public EventStorePersistentSubscriptionsClient Client               { get; }
	public EventStoreClient                        StreamsClient        { get; }
	public EventStoreUserManagementClient          UserManagementClient { get; }

	protected override async Task OnServerUpAsync() {
		await StreamsClient.WarmUp();
		await UserManagementClient.WarmUp();

		if (!_skipPsWarmUp)
			await Client.WarmUp();

		await UserManagementClient.CreateUserWithRetry(
			TestCredentials.TestUser1.Username!,
			TestCredentials.TestUser1.Username!,
			Array.Empty<string>(),
			TestCredentials.TestUser1.Password!,
			TestCredentials.Root
		);
	}

	public override async Task DisposeAsync() {
		await UserManagementClient.DisposeAsync();
		await StreamsClient.DisposeAsync();
		await Client.DisposeAsync();
		await base.DisposeAsync();
	}
}