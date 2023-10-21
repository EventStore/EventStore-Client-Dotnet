namespace EventStore.Client; 

public class get_status : IClassFixture<get_status.Fixture> {
    private readonly Fixture _fixture;

    public get_status(Fixture fixture) {
        _fixture = fixture;
    }

    [Fact]
    public async Task returns_expected_result() {
        var name   = StandardProjections.Names.First();
        var result = await _fixture.Client.GetStatusAsync(name, userCredentials: TestCredentials.TestUser1);

        Assert.NotNull(result);
        Assert.Equal(name, result!.Name);
    }

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;
        protected override Task When()  => Task.CompletedTask;
    }
}