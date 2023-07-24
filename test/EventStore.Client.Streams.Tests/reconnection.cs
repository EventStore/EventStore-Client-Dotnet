using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client {
	public class @reconnection : IClassFixture<reconnection.Fixture> {
		private readonly Fixture _fixture;

		public reconnection(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task when_the_connection_is_lost() {
			var streamName = _fixture.GetStreamName();
			var eventCount = 512;
			var receivedAllEvents = new TaskCompletionSource();
			var serverRestarted = new TaskCompletionSource();
			var receivedEvents = new List<ResolvedEvent>();
			var resubscribed = new TaskCompletionSource<StreamSubscription>();

			using var _ = await _fixture.Client.SubscribeToStreamAsync(streamName, FromStream.Start,
					EventAppeared, subscriptionDropped: SubscriptionDropped)
				.WithTimeout();

			await _fixture.Client
				.AppendToStreamAsync(streamName, StreamState.NoStream, _fixture.CreateTestEvents(eventCount))
				.WithTimeout(); // ensure we get backpressure

			_fixture.TestServer.Stop();
			await Task.Delay(TimeSpan.FromSeconds(2));

			await _fixture.TestServer.StartAsync().WithTimeout();
			serverRestarted.SetResult();

			await resubscribed.Task.WithTimeout(TimeSpan.FromSeconds(10));

			await receivedAllEvents.Task.WithTimeout(TimeSpan.FromSeconds(10));
			
			async Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				await serverRestarted.Task;
				receivedEvents.Add(e);
				if (receivedEvents.Count == eventCount) {
					receivedAllEvents.TrySetResult();
				}
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) {
				if (reason == SubscriptionDroppedReason.Disposed || ex is null) {
					return;
				}

				if (ex is not RpcException { Status.StatusCode: StatusCode.Unavailable }) {
					receivedAllEvents.TrySetException(ex);
				} else {
					var _ = ResubscribeAsync();
				}
			}

			async Task ResubscribeAsync() {
				try {
					var sub = await _fixture.Client.SubscribeToStreamAsync(
						streamName,
						receivedEvents.Any()
							? FromStream.After(receivedEvents[^1].OriginalEventNumber)
							: FromStream.Start,
						EventAppeared,
						subscriptionDropped: SubscriptionDropped);
					resubscribed.SetResult(sub);
				} catch (Exception ex) {
					ex = ex.GetBaseException();

					if (ex is RpcException) {
						await Task.Delay(200);
						var _ = ResubscribeAsync();
					} else {
						resubscribed.SetException(ex);
					}
				}
			}
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override Task When() => Task.CompletedTask;

			public Fixture() : base(env: new() {
				["EVENTSTORE_MEM_DB"] = "false"
			}) {
				Settings.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromMilliseconds(100);
				Settings.ConnectivitySettings.GossipTimeout = TimeSpan.FromMilliseconds(100);
			}
		}
	}
}
