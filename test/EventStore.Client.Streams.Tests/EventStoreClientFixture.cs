namespace EventStore.Client.Streams.Tests; 

public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
	protected EventStoreClientFixture(
		EventStoreClientSettings? settings = null,
		Dictionary<string, string>? env = null, bool noDefaultCredentials = false
	)
		: base(settings, env, noDefaultCredentials) =>
		Client = new(Settings);

	public EventStoreClient Client { get; }

	protected override async Task OnServerUpAsync() => await Client.WarmUp();

	public override async Task DisposeAsync() {
		await Client.DisposeAsync();
		await base.DisposeAsync();
	}
}