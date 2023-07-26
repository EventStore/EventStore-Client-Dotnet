using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.Bugs {
	public class Issue_104 : IClassFixture<Issue_104.Fixture> {
		private readonly Fixture _fixture;

		public Issue_104(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task Callback_API_subscription_does_not_send_checkpoint_reached_after_disposal() {
			var streamName = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var ignoredStreamName = $"ignore_{streamName}_{Guid.NewGuid()}";
			var subscriptionDisposed = new TaskCompletionSource<bool>();
			var eventAppeared = new TaskCompletionSource<bool>();
			var checkpointReachAfterDisposed = new TaskCompletionSource<bool>();

			await _fixture.Client.AppendToStreamAsync(streamName, StreamRevision.None, _fixture.CreateTestEvents());

			var subscription = await _fixture.Client.SubscribeToAllAsync(
				FromAll.Start,
				(_, _, _) => {
					eventAppeared.TrySetResult(true);
					return Task.CompletedTask;
				}, false, (_, _, _) => subscriptionDisposed.TrySetResult(true), new SubscriptionFilterOptions(
					StreamFilter.Prefix(streamName), 1, (_, _, _) => {
						if (!subscriptionDisposed.Task.IsCompleted) {
							return Task.CompletedTask;
						}

						checkpointReachAfterDisposed.TrySetResult(true);
						return Task.CompletedTask;
					}), userCredentials: new UserCredentials("admin", "changeit"));

			await eventAppeared.Task;

			subscription.Dispose();
			await subscriptionDisposed.Task;

			await _fixture.Client.AppendToStreamAsync(ignoredStreamName, StreamRevision.None,
				_fixture.CreateTestEvents(50));

			var delay = Task.Delay(300);
			var result = await Task.WhenAny(delay, checkpointReachAfterDisposed.Task);
			Assert.Equal(delay, result); // iow 300ms have passed without seeing checkpointReachAfterDisposed
		}
		
		[Fact]
		public async Task Iterator_API_subscription_does_not_send_checkpoint_reached_after_disposal() {
			var streamName = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var ignoredStreamName = $"ignore_{streamName}_{Guid.NewGuid()}";
			var subscriptionDisposed = new TaskCompletionSource<bool>();
			var eventAppeared = new TaskCompletionSource<bool>();
			var checkpointReachAfterDisposed = new TaskCompletionSource<bool>();

			await _fixture.Client.AppendToStreamAsync(streamName, StreamRevision.None, _fixture.CreateTestEvents());

			var subscription = _fixture.Client.SubscribeToAll(FromAll.Start,false, new SubscriptionFilterOptions(StreamFilter.Prefix(streamName)));

			ReadMessages(subscription, _ => {
				eventAppeared.TrySetResult(true);
				return Task.CompletedTask;
			}, _ => subscriptionDisposed.TrySetResult(true), _ => {
				if (!subscriptionDisposed.Task.IsCompleted) {
					return Task.CompletedTask;
				}

				checkpointReachAfterDisposed.TrySetResult(true);
				return Task.CompletedTask;
			});
			
			await eventAppeared.Task;

			subscription.Dispose();
			await subscriptionDisposed.Task;

			await _fixture.Client.AppendToStreamAsync(ignoredStreamName, StreamRevision.None,
				_fixture.CreateTestEvents(50));

			var delay = Task.Delay(300);
			var result = await Task.WhenAny(delay, checkpointReachAfterDisposed.Task);
			Assert.Equal(delay, result); // iow 300ms have passed without seeing checkpointReachAfterDisposed
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override Task When() => Task.CompletedTask;
		}
		
		async void ReadMessages(EventStoreClient.SubscriptionResult subscription, Func<ResolvedEvent, Task> eventAppeared, Action<Exception?> subscriptionDropped, Func<Position, Task> checkpointReached) {
			Exception? exception = null;
			try {
				await foreach (var message in subscription.Messages) {
					if (message is StreamMessage.Event eventMessage) {
						await eventAppeared(eventMessage.ResolvedEvent);
					} else if (message is StreamMessage.SubscriptionMessage.Checkpoint checkpointMessage) {
						await checkpointReached(checkpointMessage.Position);
					}
				}
			} catch (Exception ex) {
				exception = ex;
			}

			//allow some time for subscription cleanup and chance for exception to be raised
			await Task.Delay(100);

			try {
				//subscription.SubscriptionState will throw exception if some problem occurred for the subscription
				Assert.Equal(SubscriptionState.Disposed, subscription.SubscriptionState);
				subscriptionDropped(exception);
			} catch (Exception ex) {
				subscriptionDropped(ex);
			}
		}
	}
}
