namespace EventStore.Client.Streams.Tests.Append;

[Trait("Category", "Target:Stream")]
[Trait("Category", "Operation:Append")]
public class append_to_stream_with_tls_ca_file(ITestOutputHelper output, EventStoreFixture fixture)
	: EventStoreTests<EventStoreFixture>(output, fixture) {
	public static IEnumerable<object[]> CertPaths =>
		new List<object[]> {
			// relative
			new object[] { Path.Combine("certs", "ca", "ca.crt") },

			// absolute
			new object[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "ca", "ca.crt") },
		};

	[Theory]
	[MemberData(nameof(CertPaths))]
	private async Task TestAppendWithCaFile(string certificateFilePath) {
		Fixture.Log.Information($"Using certificate: {certificateFilePath}");

		var connectionString =
			$"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&tlsCAFile={certificateFilePath}";

		var settings = EventStoreClientSettings.Create(connectionString);

		var client = new EventStoreClient(settings);

		var appendResult = await client.AppendToStreamAsync(
			"some-stream",
			StreamState.Any,
			new[] { new EventData(Uuid.NewUuid(), "some-event", default) }
		);

		appendResult.ShouldNotBeNull();

		await client.DisposeAsync();
	}
}
