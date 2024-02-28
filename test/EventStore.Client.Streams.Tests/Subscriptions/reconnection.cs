using Grpc.Core;
using static System.TimeSpan;

namespace EventStore.Client.Streams.Tests.Subscriptions; 

[Trait("Category", "Subscriptions")]
public class @reconnection(ITestOutputHelper output, ReconnectionFixture fixture) : EventStoreTests<ReconnectionFixture>(output, fixture) {
	[Theory]
	[InlineData(4, 5000, 0, 30000)]
	public async Task when_the_connection_is_lost(int expectedNumberOfEvents, int reconnectDelayMs, int serviceRestartDelayMs, int testTimeoutMs) {
		using var cancellator = new CancellationTokenSource().With(x => x.CancelAfter(testTimeoutMs));

		var streamName = Fixture.GetStreamName();
		
		// create backpressure by producing half of the events
		await Fixture.ProduceEvents(streamName, expectedNumberOfEvents / 2, cancellationToken: cancellator.Token);
		
		// create subscription that will actually receive the first event and
		// then wait for the service to be restarted
		// but we are evil and will force the drop of the subscription muah ah ah
		var consumeEvents = Fixture.ConsumeEvents(
			streamName,
			expectedNumberOfEvents,
			FromMilliseconds(reconnectDelayMs),
			cancellator.Token
		);
		
		// create chaos by pausing the service
		await Fixture.RestartService(FromMilliseconds(serviceRestartDelayMs));
	
		// produce the rest of the events to make it more interesting
		await Fixture.ProduceEvents(streamName, expectedNumberOfEvents / 2, cancellationToken: cancellator.Token);
		
		// wait for the subscription to receive all events or timeout
		await consumeEvents.ShouldNotThrowAsync();
	}
}

public class ReconnectionFixture()
	: EventStoreFixture(
		x => x.RunInMemory(false)
			.With(o => o.ClientSettings.ConnectivitySettings.DiscoveryInterval = FromMilliseconds(100))
			.With(o => o.ClientSettings.ConnectivitySettings.GossipTimeout = FromMilliseconds(100))
	) 
{
	public async Task ProduceEvents(string streamName, int numberOfEvents, StreamState? streamState = null, CancellationToken cancellationToken = default) {
		while (!cancellationToken.IsCancellationRequested) {
			try {
				var result = await Streams.AppendToStreamAsync(
					streamName,
					streamState.GetValueOrDefault(StreamState.Any),
					CreateTestEvents(numberOfEvents),
					cancellationToken: cancellationToken
				);

				if (result is SuccessResult success) {
					Log.Information(
						"{NumberOfEvents} events produced to {StreamName}.", numberOfEvents, streamName
					);

					return;
				}

				Log.Error(
					"Failed to produce {NumberOfEvents} events to {StreamName}.", numberOfEvents, streamName
				);

				await Task.Delay(250);
			}
			catch (Exception ex) when ( ex is not OperationCanceledException) {
				Log.Error(
					ex, "Failed to produce {NumberOfEvents} events to {StreamName}.", numberOfEvents, streamName
				);

				await Task.Delay(250);
			}
		}
	}

	public Task ConsumeEvents(
		string streamName, 
		int expectedNumberOfEvents, 
		TimeSpan reconnectDelay, 
		CancellationToken cancellationToken
	) {
		var receivedAllEvents = new TaskCompletionSource();
		
		var receivedEventsCount = 0;
		
		_ = SubscribeToStream(
			streamName, 
			checkpoint: null,
			OnReceive(),
			OnDrop(),
			cancellationToken
		);

		return receivedAllEvents.Task;

		Func<ResolvedEvent, Task> OnReceive() {
			return re => {
				receivedEventsCount++;
				Log.Debug("{ReceivedEventsCount}/{ExpectedNumberOfEvents} events received.", receivedEventsCount, expectedNumberOfEvents);

				if (receivedEventsCount == expectedNumberOfEvents) {
					Log.Information("Test complete. {ReceivedEventsCount}/{ExpectedNumberOfEvents} events received.", receivedEventsCount, expectedNumberOfEvents);
					receivedAllEvents.TrySetResult();
				}
				
				return Task.CompletedTask;
			};
		}

		Func<SubscriptionDroppedReason, Exception?, Task<bool>> OnDrop() {
			return async (reason, ex) => {
				if (ex is RpcException { StatusCode: StatusCode.Unavailable or StatusCode.DeadlineExceeded }) {
					Log.Warning("Transitive exception detected. Retrying connection in {reconnectDelayMs}ms.", reconnectDelay.TotalMilliseconds);
					await Task.Delay(reconnectDelay);
					return true;
				}
				
				if (reason == SubscriptionDroppedReason.Disposed || ex is OperationCanceledException || ex is TaskCanceledException || ex is null) {
					if (receivedEventsCount != expectedNumberOfEvents)
						receivedAllEvents.TrySetException(new TimeoutException($"Test timeout detected. {receivedEventsCount}/{expectedNumberOfEvents} events received.", ex));
					else {
						Log.Information("Test cancellation requested. {ReceivedEventsCount}/{ExpectedNumberOfEvents} events received.", receivedEventsCount, expectedNumberOfEvents);
						receivedAllEvents.TrySetCanceled(cancellationToken);
					}
					
					return false;
				}
			
				Log.Fatal(ex, "Fatal exception detected. This is the end...");
				receivedAllEvents.SetException(ex);

				return false;
			};
		}
	}
	
	async Task SubscribeToStream(
		string stream, 
		StreamPosition? checkpoint, 
		Func<ResolvedEvent, Task> onReceive,
		Func<SubscriptionDroppedReason, Exception?, Task<bool>> onDrop,
		CancellationToken cancellationToken
	) {
		var start = checkpoint == null ? FromStream.Start : FromStream.After(checkpoint.Value);
		
		Log.Verbose("Attempting to start from checkpoint: {Checkpoint}.", checkpoint);
		
		try {
			var sub = await Streams.SubscribeToStreamAsync(
				streamName: stream,
				start: start,
				eventAppeared: async (s, re, ct) => {
					await onReceive(re);
					checkpoint = re.OriginalEventNumber;
					Log.Verbose("Checkpoint Set: {Checkpoint}.", checkpoint);
				},
				subscriptionDropped:  async (s, reason, ex) => {
					var resubscribe    = await onDrop(reason, ex);
					if (resubscribe) _ = SubscribeToStream(stream, checkpoint, onReceive, onDrop, cancellationToken);
				},
				cancellationToken: cancellationToken
			);
		} catch (Exception ex) {
			var reason = ex is OperationCanceledException or TaskCanceledException
				? SubscriptionDroppedReason.Disposed
				: SubscriptionDroppedReason.SubscriberError;

			var resubscribe    = await onDrop(reason, ex);
			if (resubscribe) _ = SubscribeToStream(stream, checkpoint, onReceive, onDrop, cancellationToken);
		}
	}
}