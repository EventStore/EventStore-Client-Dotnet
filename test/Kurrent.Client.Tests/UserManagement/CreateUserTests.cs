using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class CreateUserTests(ITestOutputHelper output, CreateUserTests.CustomFixture fixture)
	: KurrentPermanentTests<CreateUserTests.CustomFixture>(output, fixture) {
	[Theory, CreateUserNullInputCases]
	public async Task creating_user_with_null_input_throws(string loginName, string fullName, string[] groups, string password, string paramName) {
		var ex = await Fixture.Users
			.CreateUserAsync(loginName, fullName, groups, password, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Theory, CreateUserEmptyInputCases]
	public async Task creating_user_with_empty_input_throws(string loginName, string fullName, string[] groups, string password, string paramName) {
		var ex = await Fixture.Users
			.CreateUserAsync(loginName, fullName, groups, password, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Fact]
	public async Task creating_user_with_password_containing_ascii_chars() {
		var user = Fakers.Users.Generate();

		await Fixture.Users
			.CreateUserAsync(user.LoginName, user.FullName, user.Groups, user.Password, userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}

	[Theory]
	[ClassData(typeof(InvalidCredentialsTestCases))]
	public async Task creating_user_with_insufficient_credentials_throws(InvalidCredentialsTestCase testCase) =>
		await Fixture.Users
			.CreateUserAsync(
				testCase.User.LoginName,
				testCase.User.FullName,
				testCase.User.Groups,
				testCase.User.Password,
				userCredentials: testCase.User.Credentials
			)
			.ShouldThrowAsync(testCase.ExpectedException);

	[Fact]
	public async Task creating_user_can_be_read() {
		var user = Fakers.Users.Generate();

		await Fixture.Users
			.CreateUserAsync(
				user.LoginName,
				user.FullName,
				user.Groups,
				user.Password,
				userCredentials: TestCredentials.Root
			)
			.ShouldNotThrowAsync();

		var actual = await Fixture.Users.GetUserAsync(user.LoginName, userCredentials: TestCredentials.Root);

		var expected = new UserDetails(
			user.Details.LoginName,
			user.Details.FullName,
			user.Details.Groups,
			user.Details.Disabled,
			actual.DateLastUpdated
		);

		actual.ShouldBeEquivalentTo(expected);
	}

	class CreateUserNullInputCases : TestCaseGenerator<CreateUserNullInputCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [null!, Faker.Person.UserName, Faker.Lorem.Words(), Faker.Internet.Password(), "loginName"];
			yield return [Faker.Person.UserName, null!, Faker.Lorem.Words(), Faker.Internet.Password(), "fullName"];
			yield return [Faker.Person.UserName, Faker.Person.FullName, null!, Faker.Internet.Password(), "groups"];
			yield return [Faker.Person.UserName, Faker.Person.FullName, Faker.Lorem.Words(), null!, "password"];
		}
	}

	class CreateUserEmptyInputCases : TestCaseGenerator<CreateUserEmptyInputCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [string.Empty, Faker.Person.UserName, Faker.Lorem.Words(), Faker.Internet.Password(), "loginName"];
			yield return [Faker.Person.UserName, string.Empty, Faker.Lorem.Words(), Faker.Internet.Password(), "fullName"];
			yield return [Faker.Person.UserName, Faker.Person.FullName, Faker.Lorem.Words(), string.Empty, "password"];
		}
	}

	public class CustomFixture() : KurrentPermanentFixture(x => x.WithoutDefaultCredentials());
}
