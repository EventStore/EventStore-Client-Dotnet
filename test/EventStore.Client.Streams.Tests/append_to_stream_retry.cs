using Grpc.Core;
using Polly;

namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
public class append_to_stream_retry : IClassFixture<append_to_stream_retry.Fixture> {
	readonly Fixture _fixture;

	public append_to_stream_retry(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task can_retry() {
		var stream = _fixture.GetStreamName();

		// can definitely write without throwing
		var nextExpected = (await WriteAnEventAsync(StreamRevision.None)).NextExpectedStreamRevision;
		Assert.Equal(new(0), nextExpected);

		_fixture.TestServer.Stop();

		// writeTask cannot complete because ES is stopped
		var ex = await Assert.ThrowsAnyAsync<Exception>(() => WriteAnEventAsync(new(0)));
		Assert.True(
			ex is RpcException {
				Status: {
					StatusCode: StatusCode.Unavailable
				}
			} or DiscoveryException
		);

		await _fixture.TestServer.StartAsync().WithTimeout();

		// write can be retried
		var writeResult = await Policy
			.Handle<Exception>()
			.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3))
			.ExecuteAsync(async () => await WriteAnEventAsync(new(0)));

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);
		
		return;

		Task<IWriteResult> WriteAnEventAsync(StreamRevision expectedRevision) =>
			_fixture.Client.AppendToStreamAsync(
				stream,
				expectedRevision,
				_fixture.CreateTestEvents(1)
			);
	}

	public class Fixture : EventStoreClientFixture {
		public Fixture() : base(
			env: new() {
				["EVENTSTORE_MEM_DB"] = "false"
			}
		) =>
			Settings.ConnectivitySettings.MaxDiscoverAttempts = 2;

		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}