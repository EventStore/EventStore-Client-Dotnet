namespace EventStore.Client.Streams.Tests.Subscriptions;


[Trait("Category", "Subscriptions")]
public class SubscriptionsFixture : EventStoreFixture {
	public SubscriptionsFixture(): base(x => x.RunProjections()) {
		OnSetup = async () => {
			await Streams.SetStreamMetadataAsync(
				SystemStreams.AllStream,
				StreamState.NoStream,
				new(acl: new(SystemRoles.All)),
				userCredentials: TestCredentials.Root
			);
			
			await Streams.AppendToStreamAsync($"SubscriptionsFixture-Noise-{Guid.NewGuid():N}", StreamState.NoStream, CreateTestEvents(10));
		};
	}
}