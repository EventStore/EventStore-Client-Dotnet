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
		public async Task status_is_aborted_stopped_abortedstopped() {
			var name = StandardProjections.Names.First();
			await _fixture.Client.DisableAsync(name, TestCredentials.Root);
			var result = await _fixture.Client.GetStatusAsync(name, TestCredentials.Root);
			Assert.Contains(result.Status, new[] {"Aborted/Stopped", "Aborted", "Stopped"});

		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
