using Grpc.Core;

namespace EventStore.Client.SubscriptionToStream; 

public class create_duplicate
    : IClassFixture<create_duplicate.Fixture> {
    public create_duplicate(Fixture fixture) {
        _fixture = fixture;
    }

    private const    string  Stream = nameof(create_duplicate);
    private readonly Fixture _fixture;

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;

        protected override Task When() =>
            Client.CreateToStreamAsync(Stream, "group32",
                                       new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
    }

    [Fact]
    public async Task the_completion_fails() {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _fixture.Client.CreateToStreamAsync(Stream, "group32",
                                                      new PersistentSubscriptionSettings(),
                                                      userCredentials: TestCredentials.Root));
        Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
    }
}