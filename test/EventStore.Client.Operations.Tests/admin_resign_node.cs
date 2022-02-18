using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class admin_resign_node : IClassFixture<admin_resign_node.Fixture> {
		private readonly Fixture _fixture;

		public admin_resign_node(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task resign_node_does_not_throw() {
			await _fixture.Client.ResignNodeAsync(userCredentials: TestCredentials.Root);
		}

		[Fact]
		public async Task resign_node_without_credentials_throws() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.ResignNodeAsync());
		}
		
		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
