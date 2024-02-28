namespace EventStore.Client.Tests;

/// <summary>
/// The clients dont have default credentials set.
/// </summary>
[PublicAPI]
public class InsecureClientTestFixture() : EventStoreFixture(x => x.WithoutDefaultCredentials());