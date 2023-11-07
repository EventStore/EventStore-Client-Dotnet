namespace EventStore.Client.Tests;

[PublicAPI]
public class RunInMemoryTestFixture() : EventStoreFixture(x => x.RunInMemory());