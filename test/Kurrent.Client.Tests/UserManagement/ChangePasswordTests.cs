using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class ChangePasswordTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[Theory, ChangePasswordNullInputCases]
	public async Task changing_user_password_with_null_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Theory, ChangePasswordEmptyInputCases]
	public async Task changing_user_password_with_empty_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Theory(Skip = "This can't be right")]
	[ClassData(typeof(InvalidCredentialsTestCases))]
	public async Task changing_user_password_with_user_with_insufficient_credentials_throws(string loginName, UserCredentials userCredentials) {
		await Fixture.Users.CreateUserAsync(loginName, "Full Name", Array.Empty<string>(), "password", userCredentials: TestCredentials.Root);

		await Fixture.Users
			.ChangePasswordAsync(loginName, "password", "newPassword", userCredentials: userCredentials)
			.ShouldThrowAsync<AccessDeniedException>();
	}

	[Fact]
	public async Task changing_user_password_when_the_current_password_is_wrong_throws() {
		var user = await Fixture.CreateTestUser();

		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, "wrong-password", "new-password", userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<AccessDeniedException>();
	}

	[Fact]
	public async Task changing_user_password_with_correct_credentials() {
		var user = await Fixture.CreateTestUser();

		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, user.Password, "new-password", userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}

	class ChangePasswordNullInputCases : TestCaseGenerator<ChangePasswordNullInputCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [null!, Faker.Internet.Password(), Faker.Internet.Password(), "loginName"];
			yield return [Faker.Person.UserName, null!, Faker.Internet.Password(), "currentPassword"];
			yield return [Faker.Person.UserName, Faker.Internet.Password(), null!, "newPassword"];
		}
	}

	class ChangePasswordEmptyInputCases : TestCaseGenerator<ChangePasswordEmptyInputCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [string.Empty, Faker.Internet.Password(), Faker.Internet.Password(), "loginName"];
			yield return [Faker.Person.UserName, string.Empty, Faker.Internet.Password(), "currentPassword"];
			yield return [Faker.Person.UserName, Faker.Internet.Password(), string.Empty, "newPassword"];
		}
	}
}
