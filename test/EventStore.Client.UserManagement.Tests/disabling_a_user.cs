namespace EventStore.Client.Tests; 

public class disabling_a_user : EventStoreFixture {
    public disabling_a_user(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task with_null_input_throws() {
        var ex = await Fixture.Users
            .EnableUserAsync(null!, userCredentials: TestCredentials.Root)
            .ShouldThrowAsync<ArgumentNullException>();

        ex.ParamName.ShouldBe("loginName");
    }

    [Fact]
    public async Task with_empty_input_throws() {
        var ex = await Fixture.Users
            .EnableUserAsync(string.Empty, userCredentials: TestCredentials.Root)
            .ShouldThrowAsync<ArgumentOutOfRangeException>();

        ex.ParamName.ShouldBe("loginName");
    }

    [Theory, ClassData(typeof(InvalidCredentialsCases))]
    public async Task with_user_with_insufficient_credentials_throws(TestUser user, Type expectedException) {
        await Fixture.Users.CreateUserAsync(
            user.LoginName, user.FullName, user.Groups,
            user.Password, userCredentials: TestCredentials.Root
        );

        await Fixture.Users
            .DisableUserAsync(user.LoginName, userCredentials: user.Credentials)
            .ShouldThrowAsync(expectedException);
    }

    [Fact]
    public async Task that_was_disabled() {
        var user = await Fixture.CreateTestUser();

        await Fixture.Users
            .DisableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
            .ShouldNotThrowAsync();

        await Fixture.Users
            .DisableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
            .ShouldNotThrowAsync();
    }

    [Fact]
    public async Task that_is_enabled() {
        var user = await Fixture.CreateTestUser();

        await Fixture.Users
            .DisableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
            .ShouldNotThrowAsync();
    }
}