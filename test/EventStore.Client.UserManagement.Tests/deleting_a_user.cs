using System;
using System.Threading.Tasks;
using EventStore.Tests.Fixtures;

namespace EventStore.Client {
	public class deleting_a_user : IClassFixture<NoCredentialsEventStoreIntegrationFixture> {
		readonly NoCredentialsEventStoreIntegrationFixture _fixture;

		public deleting_a_user(NoCredentialsEventStoreIntegrationFixture fixture) => _fixture = fixture;

		[Fact]
		public async Task with_null_input_throws() {
			var ex = await Assert.ThrowsAsync<ArgumentNullException>(
				() => _fixture.Client.DeleteUserAsync(null!, userCredentials: TestCredentials.Root));
			Assert.Equal("loginName", ex.ParamName);
		}

		[Fact]
		public async Task with_empty_input_throws() {
			var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
				() => _fixture.Client.DeleteUserAsync(string.Empty, userCredentials: TestCredentials.Root));
			Assert.Equal("loginName", ex.ParamName);
		}

		[Theory, ClassData(typeof(InvalidCredentialsCases))]
		public async Task with_user_with_insufficient_credentials_throws(string loginName,
			UserCredentials userCredentials) {
			await _fixture.Client.CreateUserAsync(loginName, "Full Name", new[] {"foo", "bar"},
				"password", userCredentials: TestCredentials.Root);
			if(userCredentials == null)
				await Assert.ThrowsAsync<AccessDeniedException>(
				() => _fixture.Client.DeleteUserAsync(loginName));
			else
				await Assert.ThrowsAsync<NotAuthenticatedException>(
				() => _fixture.Client.DeleteUserAsync(loginName, userCredentials: userCredentials));
		}

		[Fact]
		public async Task cannot_be_read() {
			var loginName = Guid.NewGuid().ToString();
			await _fixture.Client.CreateUserAsync(loginName, "Full Name", new[] {"foo", "bar"}, "password",
				userCredentials: TestCredentials.Root);

			await _fixture.Client.DeleteUserAsync(loginName, userCredentials: TestCredentials.Root);

			var ex = await Assert.ThrowsAsync<UserNotFoundException>(
				() => _fixture.Client.GetUserAsync(loginName, userCredentials: TestCredentials.Root));

			Assert.Equal(loginName, ex.LoginName);
		}

		[Fact]
		public async Task a_second_time_throws() {
			var loginName = Guid.NewGuid().ToString();
			await _fixture.Client.CreateUserAsync(loginName, "Full Name", new[] {"foo", "bar"}, "password",
				userCredentials: TestCredentials.Root);

			await _fixture.Client.DeleteUserAsync(loginName, userCredentials: TestCredentials.Root);

			var ex = await Assert.ThrowsAsync<UserNotFoundException>(
				() => _fixture.Client.DeleteUserAsync(loginName, userCredentials: TestCredentials.Root));

			Assert.Equal(loginName, ex.LoginName);
		}
	}
}
