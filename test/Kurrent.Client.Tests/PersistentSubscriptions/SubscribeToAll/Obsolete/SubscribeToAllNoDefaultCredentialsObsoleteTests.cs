using EventStore.Client;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllNoDefaultCredentialsObsoleteTests(ITestOutputHelper output, SubscribeToAllNoDefaultCredentialsObsoleteTests.CustomFixture fixture)
	: KurrentPermanentTests<SubscribeToAllNoDefaultCredentialsObsoleteTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_without_permissions() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToAllAsync(
					group,
					delegate { return Task.CompletedTask; }
				);
			}
		);
	}

	[RetryFact]
	public async Task throws_persistent_subscription_not_found() {
		var group = Fixture.GetGroupName();

		var ex = await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToAllAsync(
					group,
					delegate { return Task.CompletedTask; },
					userCredentials: TestCredentials.Root
				);
			}
		).WithTimeout();

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
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
