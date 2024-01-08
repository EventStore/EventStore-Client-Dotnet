namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class list_with_persistent_subscriptions : IClassFixture<list_with_persistent_subscriptions.Fixture> {
	const int    AllStreamSubscriptionCount = 3;
	const int    StreamSubscriptionCount    = 4;
	const string GroupName                  = nameof(list_with_persistent_subscriptions);
	const string StreamName                 = nameof(list_with_persistent_subscriptions);

	readonly Fixture _fixture;

	public list_with_persistent_subscriptions(Fixture fixture) => _fixture = fixture;

	int TotalSubscriptionCount =>
		SupportsPSToAll.No
			? StreamSubscriptionCount
			: StreamSubscriptionCount + AllStreamSubscriptionCount;

	[Fact]
	public async Task throws_when_not_supported() {
		if (SupportsPSToAll.No)
			await Assert.ThrowsAsync<NotSupportedException>(async () => { await _fixture.Client.ListToAllAsync(userCredentials: TestCredentials.Root); });
	}

	[SupportsPSToAll.Fact]
	public async Task returns_subscriptions_to_all_stream() {
		var result = (await _fixture.Client.ListToAllAsync(userCredentials: TestCredentials.Root)).ToList();
		Assert.Equal(AllStreamSubscriptionCount, result.Count);
		Assert.All(result, s => Assert.Equal("$all", s.EventSource));
	}

	[Fact]
	public async Task returns_all_subscriptions() {
		var result = (await _fixture.Client.ListAllAsync(userCredentials: TestCredentials.Root)).ToList();
		Assert.Equal(TotalSubscriptionCount, result.Count());
	}

	[SupportsPSToAll.Fact]
	public async Task throws_with_no_credentials() =>
		await Assert.ThrowsAsync<AccessDeniedException>(
			async () =>
				await _fixture.Client.ListToAllAsync()
		);

	[SupportsPSToAll.Fact]
	public async Task throws_with_non_existing_user() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(
			async () =>
				await _fixture.Client.ListToAllAsync(userCredentials: TestCredentials.TestBadUser)
		);

	[SupportsPSToAll.Fact]
	public async Task returns_result_with_normal_user_credentials() {
		var result = await _fixture.Client.ListToAllAsync(userCredentials: TestCredentials.TestUser1);
		Assert.Equal(AllStreamSubscriptionCount, result.Count());
	}

	public class Fixture : EventStoreClientFixture {
		public Fixture() : base(skipPSWarmUp: true, noDefaultCredentials: true) { }

		protected override async Task Given() {
			for (var i = 0; i < StreamSubscriptionCount; i++)
				await Client.CreateToStreamAsync(
					StreamName,
					GroupName + i,
					new(),
					userCredentials: TestCredentials.Root
				);

			if (SupportsPSToAll.No)
				return;

			for (var i = 0; i < AllStreamSubscriptionCount; i++)
				await Client.CreateToAllAsync(
					GroupName + i,
					new(),
					userCredentials: TestCredentials.Root
				);
		}

		protected override Task When() => Task.CompletedTask;
	}
}