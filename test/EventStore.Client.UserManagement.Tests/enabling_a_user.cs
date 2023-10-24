namespace EventStore.Client.Tests; 

public class enabling_a_user : EventStoreFixture {
    public enabling_a_user(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public async Task with_null_input_throws() {
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => Fixture.Users.EnableUserAsync(null!, userCredentials: TestCredentials.Root));
        Assert.Equal("loginName", ex.ParamName);
    }

    [Fact]
    public async Task with_empty_input_throws() {
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => Fixture.Users.EnableUserAsync(string.Empty, userCredentials: TestCredentials.Root)
        );

        Assert.Equal("loginName", ex.ParamName);
    }

    [Theory, ClassData(typeof(InvalidCredentialsCases))]
    public async Task with_user_with_insufficient_credentials_throws(TestUser user, Type expectedException) {
        await Fixture.Users.CreateUserAsync(
            user.LoginName, user.FullName, user.Groups,
            user.Password, userCredentials: TestCredentials.Root
        );

        await Fixture.Users
            .EnableUserAsync(user.LoginName, userCredentials: user.Credentials)
            .ShouldThrowAsync(expectedException);
    }

    [Fact]
    public async Task that_was_disabled() {
        var loginName = Guid.NewGuid().ToString();
        await Fixture.Users.CreateUserAsync(
            loginName, "Full Name", new[] {
                "foo",
                "bar"
            }, "password", userCredentials: TestCredentials.Root
        );

        await Fixture.Users.DisableUserAsync(loginName, userCredentials: TestCredentials.Root);
        await Fixture.Users.EnableUserAsync(loginName, userCredentials: TestCredentials.Root);
    }

    [Fact]
    public async Task that_is_enabled() {
        var loginName = Guid.NewGuid().ToString();
        await Fixture.Users.CreateUserAsync(
            loginName, "Full Name", new[] {
                "foo",
                "bar"
            }, "password", userCredentials: TestCredentials.Root
        );

        await Fixture.Users.EnableUserAsync(loginName, userCredentials: TestCredentials.Root);
    }
}