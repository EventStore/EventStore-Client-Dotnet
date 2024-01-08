namespace EventStore.Client.PersistentSubscriptions.Tests;

public class restart_subsystem : IClassFixture<restart_subsystem.Fixture> {
	readonly Fixture _fixture;

	public restart_subsystem(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task does_not_throw() => await _fixture.Client.RestartSubsystemAsync(userCredentials: TestCredentials.Root);

	[Fact]
	public async Task throws_with_no_credentials() =>
		await Assert.ThrowsAsync<AccessDeniedException>(
			async () =>
				await _fixture.Client.RestartSubsystemAsync()
		);

	[Fact(Skip = "Unable to produce same behavior with HTTP fallback!")]
	public async Task throws_with_non_existing_user() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(
			async () => await _fixture.Client.RestartSubsystemAsync(userCredentials: TestCredentials.TestBadUser)
		);

	[Fact]
	public async Task throws_with_normal_user_credentials() =>
		await Assert.ThrowsAsync<AccessDeniedException>(async () => await _fixture.Client.RestartSubsystemAsync(userCredentials: TestCredentials.TestUser1));

	public class Fixture : EventStoreClientFixture {
		public Fixture() : base(noDefaultCredentials: true) { }

		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}

// namespace EventStore.Client.PersistentSubscriptions.Tests;
//
// public class restart_subsystem : IClassFixture<InsecureClientTestFixture> {
// 	readonly InsecureClientTestFixture _fixture;
//
// 	public restart_subsystem(InsecureClientTestFixture fixture) => _fixture = fixture;
//
// 	[Fact]
// 	public async Task does_not_throw() => 
// 		await _fixture.PersistentSubscriptions.RestartSubsystemAsync(userCredentials: TestCredentials.Root);
//
// 	[Fact]
// 	public async Task throws_with_no_credentials() =>
// 		await Assert.ThrowsAsync<AccessDeniedException>(
// 			async () =>
// 				await _fixture.PersistentSubscriptions.RestartSubsystemAsync()
// 		);
//
// 	[Fact(Skip = "Unable to produce same behavior with HTTP fallback!")]
// 	public async Task throws_with_non_existing_user() =>
// 		await Assert.ThrowsAsync<NotAuthenticatedException>(
// 			async () => await _fixture.PersistentSubscriptions.RestartSubsystemAsync(userCredentials: TestCredentials.TestBadUser)
// 		);
//
// 	[Fact]
// 	public async Task throws_with_normal_user_credentials() {
// 		await _fixture.Users.CreateUserWithRetry(
// 			TestCredentials.TestUser1.Username!,
// 			TestCredentials.TestUser1.Username!,
// 			Array.Empty<string>(),
// 			TestCredentials.TestUser1.Password!,
// 			TestCredentials.Root
// 		);
// 		
// 		await Assert.ThrowsAsync<AccessDeniedException>(
// 			async () => await _fixture.PersistentSubscriptions.RestartSubsystemAsync(userCredentials: TestCredentials.TestUser1)
// 		);
// 	}
// }