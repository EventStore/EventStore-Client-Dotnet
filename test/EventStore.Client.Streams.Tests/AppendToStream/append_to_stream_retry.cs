using Polly;

namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Network")]
[Trait("Category", "DedicatedDatabase")]
[Trait("Category", "Stream")]
[Trait("Category", "Append")]
public class append_to_stream_retry : IClassFixture<EventStoreFixture> {
	public append_to_stream_retry(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task can_retry() {
		var stream = Fixture.GetStreamName();

		// can definitely write without throwing
		var result = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamRevision.None,
			Fixture.CreateTestEvents()
		);

		result.NextExpectedStreamRevision.ShouldBe(new(0));

		await Fixture.Service.Restart();

		// write can be retried
		var writeResult = await Policy
			.Handle<Exception>()
			.WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1))
			.ExecuteAsync(
				async () => await Fixture.Streams.AppendToStreamAsync(
					stream,
					result.NextExpectedStreamRevision,
					Fixture.CreateTestEvents()
				)
			);

		writeResult.NextExpectedStreamRevision.ShouldBe(new(1));
	}
}

public class StreamRetryFixture() : EventStoreFixture(
	x => x.RunInMemory(false).With(o => o.ClientSettings.ConnectivitySettings.MaxDiscoverAttempts = 2)
);