namespace EventStore.Client.Tests; 

public class getting_current_user : EventStoreFixture {
    public getting_current_user(ITestOutputHelper output) : base(output) { }
        
    [Fact]
    public async Task returns_the_current_user() {
        var user = await Fixture.Users.GetCurrentUserAsync(TestCredentials.Root);
        user.LoginName.ShouldBe(TestCredentials.Root.Username);
    }
}