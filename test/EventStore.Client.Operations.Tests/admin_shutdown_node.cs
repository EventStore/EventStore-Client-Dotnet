using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class admin_shutdown_node : IClassFixture<admin_shutdown_node.Fixture> {
		private readonly Fixture _fixture;

		public admin_shutdown_node(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task shutdown_does_not_throw() {
			await _fixture.Client.ShutdownAsync(userCredentials: TestCredentials.Root);
		}

		[Fact]
		public async Task shutdown_without_credentials_throws() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.ShutdownAsync());
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
