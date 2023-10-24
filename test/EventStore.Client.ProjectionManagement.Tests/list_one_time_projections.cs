namespace EventStore.Client;

public class list_one_time_projections : IClassFixture<list_one_time_projections.Fixture> {
    readonly Fixture _fixture;

    public list_one_time_projections(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task returns_expected_result() {
        var result = await _fixture.Client.ListOneTimeAsync(userCredentials: TestCredentials.Root)
            .ToArrayAsync();

        var details = Assert.Single(result);
        Assert.Equal("OneTime", details.Mode);
    }

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() =>
            Client.CreateOneTimeAsync("fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);

        protected override Task When() => Task.CompletedTask;
    }
}