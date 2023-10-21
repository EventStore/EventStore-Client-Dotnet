namespace EventStore.Client.Tests; 

public class getting_current_user : IClassFixture<EventStoreClientsFixture> {
    public getting_current_user(EventStoreClientsFixture fixture, ITestOutputHelper output) => 
        Fixture = fixture.With(f => f.CaptureLogs(output));

    EventStoreClientsFixture Fixture { get; }
        
    [Fact]
    public async Task returns_the_current_user() {
        var user = await Fixture.Users.GetCurrentUserAsync(TestCredentials.Root);
        user.LoginName.ShouldBe(TestCredentials.Root.Username);
    }
}