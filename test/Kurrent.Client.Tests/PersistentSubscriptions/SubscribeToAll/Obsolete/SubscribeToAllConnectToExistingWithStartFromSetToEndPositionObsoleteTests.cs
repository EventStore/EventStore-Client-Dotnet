using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllConnectToExistingWithStartFromSetToEndPositionObsoleteTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		TaskCompletionSource<ResolvedEvent> firstNonSystemEventSource = new();

		foreach (var @event in Fixture.CreateTestEvents(10)) {
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);
		}

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.End), userCredentials: TestCredentials.Root);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, r, ct) => {
				if (SystemStreams.IsSystemStream(e.OriginalStreamId)) {
					await subscription.Ack(e);
					return;
				}

				firstNonSystemEventSource.TrySetResult(e);
				await subscription.Ack(e);
			},
			(subscription, reason, ex) => {
				if (reason != SubscriptionDroppedReason.Disposed)
					firstNonSystemEventSource.TrySetException(ex!);
			},
			TestCredentials.Root
		);

		await Assert.ThrowsAsync<TimeoutException>(() => firstNonSystemEventSource.Task.WithTimeout());
	}
}
