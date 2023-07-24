using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class restart_subsystem : IClassFixture<restart_subsystem.Fixture> {
		private readonly Fixture _fixture;
		
		public restart_subsystem(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task does_not_throw() {
			await _fixture.Client.RestartSubsystemAsync(userCredentials: TestCredentials.Root);
		}

		[Fact]
		public async Task throws_with_no_credentials() {
			await Assert.ThrowsAsync<AccessDeniedException>(async () =>
				await _fixture.Client.RestartSubsystemAsync());
		}

		[Fact(Skip = "Unable to produce same behavior with HTTP fallback!")]
		public async Task throws_with_non_existing_user() {
			await Assert.ThrowsAsync<NotAuthenticatedException>(async () =>
				await _fixture.Client.RestartSubsystemAsync(userCredentials: TestCredentials.TestBadUser));
		}
		
		[Fact]
		public async Task throws_with_normal_user_credentials() {
			await Assert.ThrowsAsync<AccessDeniedException>(async () =>
				await _fixture.Client.RestartSubsystemAsync(userCredentials: TestCredentials.TestUser1));
		}
		
		public class Fixture : EventStoreClientFixture {
			public Fixture () : base(noDefaultCredentials: true) {
			}
			
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
