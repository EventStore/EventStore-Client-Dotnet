namespace EventStore.Client.Tests;

public class resetting_user_password : IClassFixture<InsecureClientTestFixture> {
	public resetting_user_password(ITestOutputHelper output, InsecureClientTestFixture fixture) => 
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }
	
	public static IEnumerable<object?[]> NullInputCases() {
		yield return Fakers.Users.Generate().WithResult(x => new object?[] { null, x.Password, "loginName" });
		yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, null, "newPassword" });
	}
	
	[Theory]
	[MemberData(nameof(NullInputCases))]
	public async Task with_null_input_throws(string loginName, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ResetPasswordAsync(loginName, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();

		ex.ParamName.ShouldBe(paramName);
	}
	
	public static IEnumerable<object?[]> EmptyInputCases() {
		yield return Fakers.Users.Generate().WithResult(x => new object?[] { string.Empty, x.Password, "loginName" });
		yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, string.Empty, "newPassword" });
	}

	[Theory]
	[MemberData(nameof(EmptyInputCases))]
	public async Task with_empty_input_throws(string loginName, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ResetPasswordAsync(loginName, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Theory]
	[ClassData(typeof(InvalidCredentialsTestCases))]
	public async Task with_user_with_insufficient_credentials_throws(InvalidCredentialsTestCase testCase) {
		await Fixture.Users.CreateUserAsync(
			testCase.User.LoginName,
			testCase.User.FullName,
			testCase.User.Groups,
			testCase.User.Password,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Users
			.ResetPasswordAsync(testCase.User.LoginName, "newPassword", userCredentials: testCase.User.Credentials)
			.ShouldThrowAsync(testCase.ExpectedException);
	}

	[Fact]
	public async Task with_correct_credentials() {
		var user = Fakers.Users.Generate();

		await Fixture.Users.CreateUserAsync(
			user.LoginName,
			user.FullName,
			user.Groups,
			user.Password,
			userCredentials: TestCredentials.Root
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