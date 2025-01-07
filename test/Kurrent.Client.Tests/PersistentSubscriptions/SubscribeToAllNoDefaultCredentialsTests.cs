using EventStore.Client;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

public class SubscribeToAllNoDefaultCredentialsTests(ITestOutputHelper output, SubscribeToAllNoDefaultCredentialsTests.CustomFixture fixture)
	: KurrentPermanentTests<SubscribeToAllNoDefaultCredentialsTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_without_permissions() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				await using var subscription = Fixture.Subscriptions.SubscribeToAll(group);
				await subscription.AnyAsync().AsTask().WithTimeout();
			}
		);
	}

	[RetryFact]
	public async Task throws_persistent_subscription_not_found() {
		var group = Fixture.GetGroupName();

		await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		Assert.True(
			await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>().AnyAsync()
				.AsTask()
				.WithTimeout()
		);
	}

	[RetryFact]
	public async Task deleting_without_permissions() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.Subscriptions.DeleteToAllAsync(Guid.NewGuid().ToString()));
	}

	[RetryFact]
	public async Task create_without_permissions() {
		var group = Fixture.GetGroupName();

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.Subscriptions.CreateToAllAsync(
					group,
					new()
				)
		);
	}

	[RetryFact]
	public async Task update_existing_without_permissions() {
		var group = Fixture.GetGroupName();
		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() => Fixture.Subscriptions.UpdateToAllAsync(
				group,
				new()
			)
		);
	}

	[RetryFact]
	public async Task update_non_existent() {
		var group = Fixture.GetGroupName();
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			() => Fixture.Subscriptions.UpdateToAllAsync(
				group,
				new(),
				userCredentials: TestCredentials.Root
			)
		);
	}

	[RetryFact]
	public Task update_with_prepare_position_larger_than_commit_position() {
		var group = Fixture.GetGroupName();
		return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() =>
				Fixture.Subscriptions.UpdateToAllAsync(
					group,
					new(startFrom: new Position(0, 1)),
					userCredentials: TestCredentials.Root
				)
		);
	}

	public class CustomFixture() : KurrentPermanentFixture(x => x.WithoutDefaultCredentials());
}
