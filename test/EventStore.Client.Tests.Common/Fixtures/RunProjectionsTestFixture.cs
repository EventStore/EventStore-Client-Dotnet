namespace EventStore.Client.Tests;

[PublicAPI]
public class RunProjectionsTestFixture() : EventStoreFixture(x => x.RunProjections());