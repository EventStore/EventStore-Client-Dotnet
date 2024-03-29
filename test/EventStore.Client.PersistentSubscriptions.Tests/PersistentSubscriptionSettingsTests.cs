namespace EventStore.Client.PersistentSubscriptions.Tests;

public class PersistentSubscriptionSettingsTests {
	[Fact]
	public void LargeCheckpointAfterThrows() =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new PersistentSubscriptionSettings(checkPointAfter: TimeSpan.FromDays(25 * 365)));

	[Fact]
	public void LargeMessageTimeoutThrows() =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new PersistentSubscriptionSettings(messageTimeout: TimeSpan.FromDays(25 * 365)));
}