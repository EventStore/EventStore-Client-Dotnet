using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllUpdateExistingWithCheckpointObsoleteTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task update_existing_with_check_point_should_resumes_from_check_point() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		StreamPosition checkPoint = default;

		var events = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(checkPointLowerBound: 5, checkPointAfter: TimeSpan.FromSeconds(1), startFrom: StreamPosition.Start),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);

		await using var enumerator = subscription.Messages.GetAsyncEnumerator();

		await enumerator.MoveNextAsync();

		await Task.WhenAll(Subscribe().WithTimeout(), WaitForCheckpoint().WithTimeout());

		// Force restart of the subscription
		await Fixture.Subscriptions.UpdateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents(1));

		await using var sub = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);

		var resolvedEvent = await sub.Messages
			.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstAsync()
			.AsTask()
			.WithTimeout();

		Assert.Equal(checkPoint.Next(), resolvedEvent.Event.EventNumber);

		return;

		async Task Subscribe() {
			var count = 0;

			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
					continue;
				}

				count++;

				await subscription.Ack(resolvedEvent);
				if (count >= events.Length) {
					break;
				}
			}
		}

		async Task WaitForCheckpoint() {
			await using var subscription = Fixture.Streams.SubscribeToStream(
				$"$persistentsubscription-{stream}::{group}-checkpoint",
				FromStream.Start,
				userCredentials: TestCredentials.Root
			);

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event (var resolvedEvent)) {
					continue;
				}

				checkPoint = resolvedEvent.Event.Data.ParseStreamPosition();
				return;
			}
		}
	}
}
