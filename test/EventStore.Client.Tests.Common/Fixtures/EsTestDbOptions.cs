using System.Collections.Generic;
using System.IO;
using EventStore.Client;

namespace EventStore.Tests.Fixtures;

public record EsTestDbOptions(
	EventStoreClientSettings ClientSettings,
	IDictionary<string, string> Environment,
	DirectoryInfo CertificateDirectory
);