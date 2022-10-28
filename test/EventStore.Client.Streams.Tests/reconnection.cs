using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client {
	public class reconnection : IClassFixture<reconnection.Fixture> {
		private readonly Fixture _fixture;

		public reconnection(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task when_the_connection_is_lost() {
			var streamName = _fixture.GetStreamName();
			var eventCount = 512;
			var tcs = new TaskCompletionSource<object?>();
			var signal = new TaskCompletionSource<object?>();
			var events = new List<ResolvedEvent>();
			var resubscribe = new TaskCompletionSource<StreamSubscription>();

			using var _ = await _fixture.Client.SubscribeToStreamAsync(streamName, FromStream.Start,
					EventAppeared, subscriptionDropped: SubscriptionDropped)
				.WithTimeout();

			await _fixture.Client
				.AppendToStreamAsync(streamName, StreamState.NoStream, _fixture.CreateTestEvents(eventCount))
				.WithTimeout(); // ensure we get backpressure

			_fixture.TestServer.Stop();
			await Task.Delay(TimeSpan.FromSeconds(2));

			await _fixture.TestServer.StartAsync().WithTimeout();
			signal.SetResult(null);

			await resubscribe.Task.WithTimeout(TimeSpan.FromSeconds(10));

			await tcs.Task.WithTimeout(TimeSpan.FromSeconds(10));
			
			async Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				await signal.Task;
				events.Add(e);
				if (events.Count == eventCount) {
					tcs.TrySetResult(null);
				}
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) {
				if (reason == SubscriptionDroppedReason.Disposed || ex is null) {
					return;
				}

				if (ex is not RpcException {
					    Status: {
						    StatusCode: StatusCode.Unavailable
					    }
				    }) {
					tcs.TrySetException(ex);
				} else {
					Resubscribe();

					void Resubscribe() {
						var task = _fixture.Client.SubscribeToStreamAsync(streamName,
							FromStream.After(events[events.Count-1].OriginalEventNumber),
							EventAppeared, subscriptionDropped: SubscriptionDropped);
						task.ContinueWith(_ => resubscribe.SetResult(_.Result),
							TaskContinuationOptions.OnlyOnRanToCompletion);
						task.ContinueWith(_ => {
								var ex = _.Exception!.GetBaseException();

								if (ex is RpcException {
									    StatusCode: StatusCode.DeadlineExceeded
								    }) {
									Task.Delay(200).ContinueWith(_ => Resubscribe());
								} else {
									resubscribe.SetException(_.Exception!.GetBaseException());
								}
							},
							TaskContinuationOptions.OnlyOnFaulted);
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
