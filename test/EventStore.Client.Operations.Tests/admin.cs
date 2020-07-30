using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class admin : IClassFixture<admin.Fixture> {
		private readonly Fixture _fixture;

		public admin(Fixture fixture) {
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

		[Fact]
		public async Task set_node_priority_does_not_throw() {
			await _fixture.Client.SetNodePriorityAsync(1000, TestCredentials.Root);
		}

		[Fact]
		public async Task set_node_priority_without_credentials_throws() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.SetNodePriorityAsync(1000));
		}

		[Fact]
		public async Task resign_node_does_not_throw() {
			await _fixture.Client.ResignNodeAsync(TestCredentials.Root);
		}

		[Fact]
		public async Task resign_node_without_credentials_throws() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.ResignNodeAsync());
		}

		[Fact]
		public async Task merge_indexes_does_not_throw() {
			await _fixture.Client.MergeIndexesAsync(TestCredentials.Root);
		}

		[Fact]
		public async Task merge_indexes_without_credentials_throws() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.MergeIndexesAsync());
		}
		
		[Fact]
		public async Task restart_persistent_subscriptions_does_not_throw() {
			await _fixture.Client.RestartPersistentSubscriptions(TestCredentials.Root);
		}

		[Fact]
		public async Task restart_persistent_subscriptions_without_credentials_throws() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.RestartPersistentSubscriptions());
		}
		
		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
