namespace EventStore.Client.Tests; 

public class resetting_user_password : IClassFixture<EventStoreInsecureClientsFixture> {
    public resetting_user_password(EventStoreInsecureClientsFixture fixture, ITestOutputHelper output) =>
        Fixture = fixture.With(f => f.CaptureLogs(output));

    EventStoreInsecureClientsFixture Fixture { get; }

    public static IEnumerable<object?[]> NullInputCases() {
        var loginName   = "ouro";
        var newPassword = "foofoofoofoofoofoo";

        yield return new object?[] {null, newPassword, nameof(loginName)};
        yield return new object?[] {loginName, null, nameof(newPassword)};
    }

    [Theory, MemberData(nameof(NullInputCases))]
    public async Task with_null_input_throws(string loginName, string newPassword, string paramName) {
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(
            () => Fixture.Users.ResetPasswordAsync(loginName, newPassword, userCredentials: TestCredentials.Root)
        );
        Assert.Equal(paramName, ex.ParamName);
    }

    public static IEnumerable<object?[]> EmptyInputCases() {
        var loginName   = "ouro";
        var newPassword = "foofoofoofoofoofoo";

        yield return new object?[] {string.Empty, newPassword, nameof(loginName)};
        yield return new object?[] {loginName, string.Empty, nameof(newPassword)};
    }

    [Theory, MemberData(nameof(EmptyInputCases))]
    public async Task with_empty_input_throws(string loginName, string newPassword, string paramName) {
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => Fixture.Users.ResetPasswordAsync(loginName, newPassword, userCredentials: TestCredentials.Root)
        );
        Assert.Equal(paramName, ex.ParamName);
    }

    [Theory, ClassData(typeof(InvalidCredentialsCases))]
    public async Task with_user_with_insufficient_credentials_throws(TestUser user, Type expectedException) {
        await Fixture.Users.CreateUserAsync(
            user.LoginName, user.FullName, user.Groups,
            user.Password, userCredentials: TestCredentials.Root
        );

        await Fixture.Users
            .ResetPasswordAsync(user.LoginName, "newPassword", userCredentials: user.Credentials)
            .ShouldThrowAsync(expectedException);
    }

    [Fact]
    public async Task with_correct_credentials() {
        var user = Fakers.Users.Generate();

        await Fixture.Users.CreateUserAsync(
            user.LoginName, user.FullName, user.Groups,
            user.Password, userCredentials: TestCredentials.Root
        );

        await Fixture.Users
            .ResetPasswordAsync(user.LoginName, "new-password", userCredentials: TestCredentials.Root)
            .ShouldNotThrowAsync();
    }

    [Fact]
    public async Task with_own_credentials_throws() {
        var user = await Fixture.CreateTestUser();

        await Fixture.Users
            .ResetPasswordAsync(user.LoginName, "new-password", userCredentials: user.Credentials)
            .ShouldThrowAsync<AccessDeniedException>();
    }
}