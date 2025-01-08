using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamConnectToExistingWithStartFromBeginningTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_with_start_from_beginning_and_no_streamconnect_to_existing_with_start_from_not_set_and_events_in_it() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();
		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Assert.ThrowsAsync<TimeoutException>(
			() => subscription.Messages.AnyAsync(message => message is PersistentSubscriptionMessage.Event).AsTask().WithTimeout()
		);
	}
}
