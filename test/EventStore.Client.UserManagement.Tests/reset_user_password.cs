using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Tests.Fixtures;

namespace EventStore.Client {
	public class resetting_user_password : IClassFixture<NoCredentialsEventStoreIntegrationFixture> {
		readonly NoCredentialsEventStoreIntegrationFixture _fixture;

		public resetting_user_password(NoCredentialsEventStoreIntegrationFixture fixture) => _fixture = fixture;

		public static IEnumerable<object?[]> NullInputCases() {
			var loginName = "ouro";
			var newPassword = "foofoofoofoofoofoo";

			yield return new object?[] {null, newPassword, nameof(loginName)};
			yield return new object?[] {loginName, null, nameof(newPassword)};
		}

		[Theory, MemberData(nameof(NullInputCases))]
		public async Task with_null_input_throws(string loginName, string newPassword,
			string paramName) {
			var ex = await Assert.ThrowsAsync<ArgumentNullException>(
				() => _fixture.Client.ResetPasswordAsync(loginName, newPassword,
					userCredentials: TestCredentials.Root));
			Assert.Equal(paramName, ex.ParamName);
		}

		public static IEnumerable<object?[]> EmptyInputCases() {
			var loginName = "ouro";
			var newPassword = "foofoofoofoofoofoo";

			yield return new object?[] {string.Empty, newPassword, nameof(loginName)};
			yield return new object?[] {loginName, string.Empty, nameof(newPassword)};
		}

		[Theory, MemberData(nameof(EmptyInputCases))]
		public async Task with_empty_input_throws(string loginName, string newPassword,
			string paramName) {
			var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
				() => _fixture.Client.ResetPasswordAsync(loginName, newPassword,
					userCredentials: TestCredentials.Root));
			Assert.Equal(paramName, ex.ParamName);
		}

		[Theory, ClassData(typeof(InvalidCredentialsCases))]
		public async Task with_user_with_insufficient_credentials_throws(string loginName,
			UserCredentials userCredentials) {
			await _fixture.Client.CreateUserAsync(loginName, "Full Name", Array.Empty<string>(),
				"password", userCredentials: TestCredentials.Root);
			if (userCredentials == null)
				await Assert.ThrowsAsync<AccessDeniedException>(
					() => _fixture.Client.ResetPasswordAsync(loginName, "newPassword"));
			else
				await Assert.ThrowsAsync<NotAuthenticatedException>(
					() => _fixture.Client.ResetPasswordAsync(loginName, "newPassword",
						userCredentials: userCredentials));
		}

		[Fact]
		public async Task with_correct_credentials() {
			var loginName = Guid.NewGuid().ToString();
			await _fixture.Client.CreateUserAsync(loginName, "Full Name", Array.Empty<string>(),
				"password", userCredentials: TestCredentials.Root);

			await _fixture.Client.ResetPasswordAsync(loginName, "newPassword",
				userCredentials: TestCredentials.Root);
		}

		[Fact]
		public async Task with_own_credentials_throws() {
			var loginName = Guid.NewGuid().ToString();
			await _fixture.Client.CreateUserAsync(loginName, "Full Name", Array.Empty<string>(),
				"password", userCredentials: TestCredentials.Root);

			await Assert.ThrowsAsync<AccessDeniedException>(
				() => _fixture.Client.ResetPasswordAsync(loginName, "newPassword",
					userCredentials: new UserCredentials(loginName, "password")));
		}
	}
}
