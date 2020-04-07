using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class disable : IClassFixture<disable.Fixture> {
		private readonly Fixture _fixture;

		public disable(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task status_is_stopped() {
			var name = StandardProjections.Names.First();
			await _fixture.Client.DisableAsync(name, TestCredentials.Root);
			var result = await _fixture.Client.GetStatusAsync(name, TestCredentials.Root);
			Assert.Equal("Stopped", result.Status);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
