using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class DeleteUserTests(ITestOutputHelper output, DeleteUserTests.CustomFixture fixture)
	: KurrentPermanentTests<DeleteUserTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task with_null_input_throws() {
		var ex = await Fixture.Users
			.DeleteUserAsync(null!, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();

		ex.ParamName.ShouldBe("loginName");
	}

	[Fact]
	public async Task with_empty_input_throws() {
		var ex = await Fixture.Users
			.DeleteUserAsync(string.Empty, userCredentials: TestCredentials.Root)
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
			.DeleteUserAsync(testCase.User.LoginName, userCredentials: testCase.User.Credentials)
			.ShouldThrowAsync(testCase.ExpectedException);
	}

	[Fact]
	public async Task cannot_be_read() {
		var user = await Fixture.CreateTestUser();

		await Fixture.Users.DeleteUserAsync(user.LoginName, userCredentials: TestCredentials.Root);

		var ex = await Fixture.Users
			.GetUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<UserNotFoundException>();

		ex.LoginName.ShouldBe(user.LoginName);
	}

	[Fact]
	public async Task a_second_time_throws() {
		var user = await Fixture.CreateTestUser();

		await Fixture.Users.DeleteUserAsync(user.LoginName, userCredentials: TestCredentials.Root);

		var ex = await Fixture.Users
			.DeleteUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<UserNotFoundException>();

		ex.LoginName.ShouldBe(user.LoginName);
	}

	public class CustomFixture() : KurrentPermanentFixture(x => x.WithoutDefaultCredentials());
}
