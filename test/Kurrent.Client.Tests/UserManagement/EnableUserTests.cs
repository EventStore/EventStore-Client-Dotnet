namespace Kurrent.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class EnableUserTests(ITestOutputHelper output, EnableUserTests.CustomFixture fixture)
	: KurrentPermanentTests<EnableUserTests.CustomFixture>(output, fixture) {
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
			.EnableUserAsync(testCase.User.LoginName, userCredentials: testCase.User.Credentials)
			.ShouldThrowAsync(testCase.ExpectedException);
	}

	[Fact]
	public async Task that_was_disabled() {
		var user = await Fixture.CreateTestUser();

		await Fixture.Users.DisableUserAsync(user.LoginName, userCredentials: TestCredentials.Root);

		await Fixture.Users
			.EnableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}

	[Fact]
	public async Task that_is_enabled() {
		var user = await Fixture.CreateTestUser();

		await Fixture.Users
			.EnableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}

	public class CustomFixture() : KurrentPermanentFixture(x => x.WithoutDefaultCredentials());
}
