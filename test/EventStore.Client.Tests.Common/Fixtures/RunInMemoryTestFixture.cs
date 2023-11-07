namespace EventStore.Client.Tests;

public class RunInMemoryTestFixture() : EventStoreFixture(x => x.RunInMemory());