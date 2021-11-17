using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
			var nextExpected = (await _fixture.Client.AppendToStreamAsync(
					stream, StreamState.NoStream, _fixture.CreateTestEvents(1))
				.WithTimeout()).NextExpectedStreamRevision;
			Assert.Equal(new StreamRevision(0), nextExpected);

			_fixture.TestServer.Stop();

			// writeTask cannot complete because ES is stopped
			await Assert.ThrowsAnyAsync<InvalidOperationException>(() => WriteAnEventAsync(new StreamRevision(0)));

			await _fixture.TestServer.StartAsync().WithTimeout();

			// write can be retried
			var writeResult = await Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3))
				.ExecuteAsync(() => {
					StreamRevision expectedRevision = new StreamRevision(0);
					return _fixture.Client.AppendToStreamAsync(
							streamName: stream,
							expectedRevision: expectedRevision,
							eventData: _fixture.CreateTestEvents(1))
						.WithTimeout();
				});

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);
		}

		public class Fixture : EventStoreClientFixture {
			public Fixture() : base(env: new Dictionary<string, string> {
				["EVENTSTORE_MEM_DB"] = "false",
			}) {
			}

			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
