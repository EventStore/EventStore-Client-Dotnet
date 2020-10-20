using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class reset : IClassFixture<reset.Fixture> {
		private readonly Fixture _fixture;

		public reset(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task status_is_running() {
			var name = StandardProjections.Names.First();
			await _fixture.Client.ResetAsync(name, TestCredentials.Root);
			var result = await _fixture.Client.GetStatusAsync(name, TestCredentials.Root);
			Assert.Equal("Running", result.Status);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
