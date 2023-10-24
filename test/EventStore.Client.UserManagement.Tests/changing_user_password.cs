namespace EventStore.Client.Tests; 

public class changing_user_password : EventStoreFixture {
    public changing_user_password(ITestOutputHelper output) : base(output) { }

    public static IEnumerable<object?[]> NullInputCases() {
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { null, x.Password, x.Password, "loginName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, null, x.Password, "currentPassword" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, x.Password, null, "newPassword" });
	}

	[Theory, MemberData(nameof(NullInputCases))]
	public async Task with_null_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();
		
		ex.ParamName.ShouldBe(paramName);
	}

    public static IEnumerable<object?[]> EmptyInputCases() {
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { string.Empty, x.Password, x.Password, "loginName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, string.Empty, x.Password, "currentPassword" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, x.Password, string.Empty, "newPassword" });
	}

	[Theory, MemberData(nameof(EmptyInputCases))]
	public async Task with_empty_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Theory(Skip = "This can't be right"), ClassData(typeof(InvalidCredentialsCases))]
	public async Task with_user_with_insufficient_credentials_throws(string loginName, UserCredentials userCredentials) {
		await Fixture.Users.CreateUserAsync(loginName, "Full Name", Array.Empty<string>(), "password", userCredentials: TestCredentials.Root);
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.Users.ChangePasswordAsync(loginName, "password", "newPassword", userCredentials: userCredentials));
	}

	[Fact]
	public async Task when_the_current_password_is_wrong_throws() {
        var user = await Fixture.CreateTestUser();

		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, "wrong-password", "new-password", userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<AccessDeniedException>();
	}

	[Fact]
	public async Task with_correct_credentials() {
        var user = await Fixture.CreateTestUser();
		
		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, user.Password, "new-password", userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}

	[Fact]
	public async Task with_own_credentials() {
        var user = await Fixture.CreateTestUser();

		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, user.Password, "new-password", userCredentials: user.Credentials)
			.ShouldNotThrowAsync();
	}
}
