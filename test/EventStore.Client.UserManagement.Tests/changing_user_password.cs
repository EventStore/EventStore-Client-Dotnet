using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Tests.Fixtures;

namespace EventStore.Client; 

public class changing_user_password : IClassFixture<NoCredentialsEventStoreIntegrationFixture> {
	public changing_user_password(NoCredentialsEventStoreIntegrationFixture fixture) => Fixture = fixture;

	NoCredentialsEventStoreIntegrationFixture Fixture { get; }
	
	public static IEnumerable<object?[]> NullInputCases() {
		yield return new object?[] { null, TestUserInfoFaker.New().Password, TestUserInfoFaker.New().Password, "loginName"};
		yield return new object?[] { TestUserInfoFaker.New().LoginName, null, TestUserInfoFaker.New().Password, "currentPassword"};
		yield return new object?[] { TestUserInfoFaker.New().LoginName, TestUserInfoFaker.New().Password, null, "newPassword"};
	}

	[Theory, MemberData(nameof(NullInputCases))]
	public async Task with_null_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Client
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();
		
		ex.ParamName.ShouldBe(paramName);
	}

	public static IEnumerable<object?[]> EmptyInputCases() {
		yield return new object?[] { string.Empty, TestUserInfoFaker.New().Password, TestUserInfoFaker.New().Password, "loginName" };
		yield return new object?[] { TestUserInfoFaker.New().LoginName, string.Empty, TestUserInfoFaker.New().Password, "currentPassword" };
		yield return new object?[] { TestUserInfoFaker.New().LoginName, TestUserInfoFaker.New().Password, string.Empty, "newPassword" };
	}

	[Theory, MemberData(nameof(EmptyInputCases))]
	public async Task with_empty_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Client
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Theory(Skip = "This can't be right"), ClassData(typeof(InvalidCredentialsCases))]
	public async Task with_user_with_insufficient_credentials_throws(string loginName, UserCredentials userCredentials) {
		await Fixture.Client.CreateUserAsync(loginName, "Full Name", Array.Empty<string>(),
			"password", userCredentials: TestCredentials.Root);
		await Assert.ThrowsAsync<AccessDeniedException>(
			() => Fixture.Client.ChangePasswordAsync(loginName, "password", "newPassword",
				userCredentials: userCredentials));
	}

	[Fact]
	public async Task when_the_current_password_is_wrong_throws() {
		var user = TestUserInfoFaker.New();
		
		await Fixture.Client
			.CreateUserAsync(user.LoginName, user.FullName, user.Groups, user.Password, userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

		await Fixture.Client
			.ChangePasswordAsync(user.LoginName, "wrong-password", "newPassword", userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<AccessDeniedException>();
	}

	[Fact]
	public async Task with_correct_credentials() {
		var user = TestUserInfoFaker.New();

		await Fixture.Client.CreateUserAsync(user.LoginName, user.FullName, user.Groups, user.Password, userCredentials: TestCredentials.Root);
		
		await Fixture.Client
			.ChangePasswordAsync(user.LoginName, user.Password, "newPassword", userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}

	[Fact]
	public async Task with_own_credentials() {
		var user = TestUserInfoFaker.New();

		await Fixture.Client
			.CreateUserAsync(user.LoginName, user.FullName, user.Groups, user.Password, userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

		var ownCredentials = new UserCredentials(user.LoginName, user.Password);
		
		await Fixture.Client
			.ChangePasswordAsync(user.LoginName, user.Password, "newPassword", userCredentials: ownCredentials)
			.ShouldNotThrowAsync();
	}
}
