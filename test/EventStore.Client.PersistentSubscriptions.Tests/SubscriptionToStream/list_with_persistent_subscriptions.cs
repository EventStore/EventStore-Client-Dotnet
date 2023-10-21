namespace EventStore.Client.SubscriptionToStream; 

public class list_with_persistent_subscriptions : IClassFixture<list_with_persistent_subscriptions.Fixture> {
    private readonly Fixture _fixture;
    private const    int     AllStreamSubscriptionCount = 4;
    private const    int     StreamSubscriptionCount    = 3;
    private const    string  GroupName                  = nameof(list_with_persistent_subscriptions);
    private const    string  StreamName                 = nameof(list_with_persistent_subscriptions);

    public list_with_persistent_subscriptions(Fixture fixture) {
        _fixture = fixture;
    }

    private int TotalSubscriptionCount => SupportsPSToAll.No
        ? StreamSubscriptionCount
        : AllStreamSubscriptionCount + StreamSubscriptionCount;
		
    [Fact]
    public async Task returns_subscriptions_to_stream() {
        var result = (await _fixture.Client.ListToStreamAsync(StreamName, userCredentials: TestCredentials.Root)).ToList();
        Assert.Equal(StreamSubscriptionCount, result.Count);
        Assert.All(result, p => Assert.Equal(StreamName, p.EventSource));
    }

    [Fact]
    public async Task returns_all_subscriptions() {
        var result = (await _fixture.Client.ListAllAsync(userCredentials: TestCredentials.Root)).ToList();
        Assert.Equal(TotalSubscriptionCount, result.Count);
    }

    [Fact]
    public async Task throws_for_non_existing() {
        await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(async () =>
                                                                              await _fixture.Client.ListToStreamAsync("NonExistingStream", userCredentials: TestCredentials.Root));
    }

    [Fact]
    public async Task throws_with_no_credentials() {
        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
                                                            await _fixture.Client.ListToStreamAsync("NonExistingStream"));
    }
		
    [Fact]
    public async Task throws_with_non_existing_user() {
        await Assert.ThrowsAsync<NotAuthenticatedException>(async () =>
                                                                await _fixture.Client.ListAllAsync(userCredentials: TestCredentials.TestBadUser));
    }
		
    [Fact]
    public async Task returns_result_with_normal_user_credentials() {
        var result = await _fixture.Client.ListAllAsync(userCredentials: TestCredentials.TestUser1);
        Assert.Equal(TotalSubscriptionCount, result.Count());
    }

    public class Fixture : EventStoreClientFixture {
        public Fixture () : base(skipPSWarmUp: true, noDefaultCredentials: true) {
        }
			
        protected override async Task Given() {
            for (int i = 0; i < StreamSubscriptionCount; i++) {
                await Client.CreateToStreamAsync(
                    StreamName,
                    GroupName + i,
                    new PersistentSubscriptionSettings(),
                    userCredentials: TestCredentials.Root);
            }

            if (SupportsPSToAll.No) {
                return;
            }
				
            for (int i = 0; i < AllStreamSubscriptionCount; i++) {
                await Client.CreateToAllAsync(
                    GroupName + i,
                    new PersistentSubscriptionSettings(),
                    userCredentials: TestCredentials.Root);
            }
        }
			
        protected override Task When() => Task.CompletedTask;
    }
}