namespace EventStore.Client.Tests;

public class TlsCaFileTests(ITestOutputHelper output, EventStoreFixture fixture) : EventStoreTests<EventStoreFixture>(output, fixture) {
	[Fact]
	public async Task tls_ca_file_exists_and_can_be_read() {
		var certificateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "ca", "ca.crt");

		Assert.True(File.Exists(certificateFilePath), $"Certificate file not found at: {certificateFilePath}");

		var connectionString = $"esdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=true&tlsCAFile={certificateFilePath}";

		var settings = EventStoreClientSettings.Create(connectionString);

		var client = new EventStoreClient(settings);

		var appendResult = await client.AppendToStreamAsync("some-stream", StreamState.Any, new[] { new EventData(Uuid.NewUuid(), "some-event", default) });
		appendResult.ShouldNotBeNull();

		var writeResult = await client.ReadStreamAsync(Direction.Forwards, "some-stream", StreamPosition.Start, 1).ToArrayAsync();
		writeResult.ShouldNotBeNull();

		await client.DisposeAsync();
	}

	[Fact]
	public void should_throw_error_when_tls_ca_file_does_not_exists() {
		var certificateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path", "not", "found");

		var connectionString = $"esdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=true&tlsCAFile={certificateFilePath}";

		var exception = Assert.ThrowsAsync<FileNotFoundException>(
			() => {
				EventStoreClientSettings.Create(connectionString);
				return Task.CompletedTask;
			}
		);

		Assert.NotNull(exception);
	}
}