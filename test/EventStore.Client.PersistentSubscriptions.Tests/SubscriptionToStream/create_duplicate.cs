using Grpc.Core;

namespace EventStore.Client.SubscriptionToStream;

public class create_duplicate
    : IClassFixture<create_duplicate.Fixture> {
    const    string  Stream = nameof(create_duplicate);
    readonly Fixture _fixture;

    public create_duplicate(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task the_completion_fails() {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _fixture.Client.CreateToStreamAsync(
                Stream,
                "group32",
                new(),
                userCredentials: TestCredentials.Root
            )
        );

        Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
    }

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;

        protected override Task When() =>
            Client.CreateToStreamAsync(
                Stream,
                "group32",
                new(),
                userCredentials: TestCredentials.Root
            );
    }
}