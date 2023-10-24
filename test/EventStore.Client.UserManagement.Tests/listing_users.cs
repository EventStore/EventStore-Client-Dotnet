namespace EventStore.Client.Tests; 

public class listing_users : EventStoreFixture {
    public listing_users(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task returns_all_created_users() {
        var seed = await Fixture.CreateTestUsers();
        
        var admin = new UserDetails("admin", "Event Store Administrator", new[] { "$admins" }, false, default);
        var ops   = new UserDetails("ops", "Event Store Operations", new[] { "$ops" }, false, default);

        var expected = new[] { admin, ops }
            .Concat(seed.Select(user => user.Details))
            //.OrderBy(user => user.LoginName)
            .ToArray();

        var actual = await Fixture.Users
            .ListAllAsync(userCredentials: TestCredentials.Root)
            //.OrderBy(user => user.LoginName)
            .Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default))
            .ToArrayAsync();

        expected.ShouldBeSubsetOf(actual);
    }
    
    [Fact]
    public async Task returns_all_system_users() {
        var admin = new UserDetails("admin", "Event Store Administrator", new[] { "$admins" }, false, default);
        var ops   = new UserDetails("ops", "Event Store Operations", new[] { "$ops" }, false, default);
    
        var expected = new[] { admin, ops };
    
        var actual = await Fixture.Users
            .ListAllAsync(userCredentials: TestCredentials.Root)
            .Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default))
            .ToArrayAsync();
    
        expected.ShouldBeSubsetOf(actual);
    }
}