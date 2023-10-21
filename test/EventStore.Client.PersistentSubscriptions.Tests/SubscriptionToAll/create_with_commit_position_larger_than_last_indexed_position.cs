using Grpc.Core;

namespace EventStore.Client.SubscriptionToAll; 

public class create_with_commit_position_larger_than_last_indexed_position
    : IClassFixture<create_with_commit_position_larger_than_last_indexed_position.Fixture> {
    public create_with_commit_position_larger_than_last_indexed_position(Fixture fixture) {
        _fixture = fixture;
    }


    private readonly Fixture _fixture;

    public class Fixture : EventStoreClientFixture {
        public ulong LastCommitPosition;
        protected override async Task Given() {
            var lastEvent = await StreamsClient.ReadAllAsync(Direction.Backwards, Position.End, 1,
                                                             userCredentials: TestCredentials.Root).FirstAsync();
            LastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new Exception();
        }
        protected override Task When() => Task.CompletedTask;
    }

    [SupportsPSToAll.Fact]
    public async Task fails() {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
                                                            _fixture.Client.CreateToAllAsync("group57",
                                                                                             new PersistentSubscriptionSettings(
                                                                                                 startFrom: new Position(_fixture.LastCommitPosition + 1, _fixture.LastCommitPosition)),
                                                                                             userCredentials: TestCredentials.Root));
        Assert.Equal(StatusCode.Internal, ex.StatusCode);
    }
}