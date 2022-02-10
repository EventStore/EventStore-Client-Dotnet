using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Polly;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class append_to_stream_retry : IClassFixture<append_to_stream_retry.Fixture> {
		private readonly Fixture _fixture;

		public append_to_stream_retry(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task can_retry() {
			var stream = _fixture.GetStreamName();

			// can definitely write without throwing
			var nextExpected = (await WriteAnEventAsync(StreamRevision.None)).NextExpectedStreamRevision;
			Assert.Equal(new StreamRevision(0), nextExpected);

			_fixture.TestServer.Stop();

			// writeTask cannot complete because ES is stopped
			var ex = await Assert.ThrowsAnyAsync<Exception>(() => WriteAnEventAsync(new StreamRevision(0)));
			Assert.True(ex is RpcException {
				Status: {
					StatusCode: StatusCode.Unavailable
				}
			} or DiscoveryException);

			await _fixture.TestServer.StartAsync().WithTimeout();

			// write can be retried
			var writeResult = await Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3))
				.ExecuteAsync(async () => await WriteAnEventAsync(new StreamRevision(0)));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			Task<IWriteResult> WriteAnEventAsync(StreamRevision expectedRevision) => _fixture.Client.AppendToStreamAsync(
				streamName: stream,
				expectedRevision: expectedRevision,
				eventData: _fixture.CreateTestEvents(1));
		}

		public class Fixture : EventStoreClientFixture {
			public Fixture() : base(env: new Dictionary<string, string> {
				["EVENTSTORE_MEM_DB"] = "false",
			}) {
				Settings.ConnectivitySettings.MaxDiscoverAttempts = 2;
			}

			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
