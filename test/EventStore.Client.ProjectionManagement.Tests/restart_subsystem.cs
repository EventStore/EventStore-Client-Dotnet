using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client {
	public class restart_subsystem : IClassFixture<restart_subsystem.Fixture> {
		private readonly Fixture _fixture;

		public restart_subsystem(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task does_not_throw() {
			await _fixture.Client.RestartSubsystemAsync(TestCredentials.Root);
		}

		[Fact]
		public async Task throws_when_given_no_credentials() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.RestartSubsystemAsync());
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
