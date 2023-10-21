namespace EventStore.Client.Tests;

public record EventStoreTestServiceOptions(
    EventStoreClientSettings ClientSettings,
    IDictionary<string, string> Environment,
    DirectoryInfo CertificateDirectory
);