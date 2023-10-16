using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Tests.Fixtures;

namespace EventStore.Client; 

class TestUserInfo {
	public string LoginName { get; set; } = null!;
	public string FullName { get; set; } = null!;
	public string[] Groups { get; set; } = null!;
	public string Password { get; set; } = null!;
}

sealed class TestUserInfoFaker : Faker<TestUserInfo> {
	static readonly TestUserInfoFaker _instance = new();

	TestUserInfoFaker() {
		RuleFor(o => o.LoginName, f => f.Person.UserName);
		RuleFor(o => o.FullName, f => f.Person.FullName);
		RuleFor(o => o.Groups, f => new[] { f.Lorem.Word(), f.Lorem.Word() });
		RuleFor(o => o.Password, () => PasswordGenerator.GeneratePassword());
	}

	public static TestUserInfo New() => _instance.Generate();
}

// public class NoCredentialsEventStoreIntegrationFixture : EventStoreIntegrationFixture {
// 	protected override IntegrationFixtureOptions Configure(IntegrationFixtureOptions options) {
// 		options.ClientSettings.DefaultCredentials = null;
// 		return options;
// 	}
// }

public class creating_a_user : IClassFixture<NoCredentialsEventStoreIntegrationFixture> {
	public creating_a_user(NoCredentialsEventStoreIntegrationFixture fixture) => Fixture = fixture;

	NoCredentialsEventStoreIntegrationFixture Fixture { get; }
	
	public static IEnumerable<object?[]> NullInputCases() {
		yield return new object?[] { null, TestUserInfoFaker.New().FullName, TestUserInfoFaker.New().Groups, TestUserInfoFaker.New().Password, "loginName" };
		yield return new object?[] { TestUserInfoFaker.New().LoginName, null, TestUserInfoFaker.New().Groups, TestUserInfoFaker.New().Password, "fullName" };
		yield return new object?[] { TestUserInfoFaker.New().LoginName, TestUserInfoFaker.New().FullName, null, TestUserInfoFaker.New().Password, "groups" };
		yield return new object?[] { TestUserInfoFaker.New().LoginName, TestUserInfoFaker.New().FullName, TestUserInfoFaker.New().Groups, null, "password" };
	}

	[Theory, MemberData(nameof(NullInputCases))]
	public async Task with_null_input_throws(string loginName, string fullName, string[] groups, string password, string paramName) {
		var ex = await Fixture.Client
			.CreateUserAsync(loginName, fullName, groups, password, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();
	
		ex.ParamName.ShouldBe(paramName);
	}

	public static IEnumerable<object?[]> EmptyInputCases() {
		yield return new object?[] { string.Empty, TestUserInfoFaker.New().FullName, TestUserInfoFaker.New().Groups, TestUserInfoFaker.New().Password, "loginName" };
		yield return new object?[] { TestUserInfoFaker.New().LoginName, string.Empty, TestUserInfoFaker.New().Groups, TestUserInfoFaker.New().Password, "fullName" };
		yield return new object?[] { TestUserInfoFaker.New().LoginName, TestUserInfoFaker.New().FullName, TestUserInfoFaker.New().Groups, string.Empty, "password" };
	}

	[Theory, MemberData(nameof(EmptyInputCases))]
	public async Task with_empty_input_throws(string loginName, string fullName, string[] groups, string password, string paramName) {
		var ex = await Fixture.Client
			.CreateUserAsync(loginName, fullName, groups, password, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}
	
	[Fact]
	public async Task with_password_containing_ascii_chars() {
		var user = TestUserInfoFaker.New();

		// await Fixture.Client.WarmUpAsync();
		//
		await Fixture.Client
			.CreateUserAsync(user.LoginName, user.FullName, user.Groups, user.Password, userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}
	
	[Theory, ClassData(typeof(InvalidCredentialsCases))]
	public async Task with_user_with_insufficient_credentials_throws(string loginName, UserCredentials? userCredentials) {
		if (userCredentials == null)
			await Fixture.Client
				.CreateUserAsync(loginName, "Full Name", new[] { "foo", "bar" }, "password")
				.ShouldThrowAsync<AccessDeniedException>();
		else
			await Fixture.Client
				.CreateUserAsync(loginName, "Full Name", new[] { "foo", "bar" }, "password", userCredentials: userCredentials)
				.ShouldThrowAsync<NotAuthenticatedException>();
	}

	[Fact]
	public async Task can_be_read() {
		var user = TestUserInfoFaker.New();

		await Fixture.Client.CreateUserAsync(
			user.LoginName, 
			user.FullName, 
			user.Groups, 
			user.Password, 
			userCredentials: TestCredentials.Root
		).ShouldNotThrowAsync();

		var actualDetails = await Fixture.Client.GetUserAsync(
			user.LoginName, 
			userCredentials: TestCredentials.Root
		);

		var expectedUserDetails = new UserDetails(
			user.LoginName, 
			user.FullName, 
			user.Groups, 
			false,
			actualDetails.DateLastUpdated
		);
		
		actualDetails.ShouldBeEquivalentTo(expectedUserDetails);
	}
}
