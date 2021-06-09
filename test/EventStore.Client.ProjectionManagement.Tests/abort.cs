using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class abort : IClassFixture<abort.Fixture> {
		private readonly Fixture _fixture;

		public abort(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task status_is_aborted_stopped_abortedstopped() {
			var name = StandardProjections.Names.First();
			await _fixture.Client.AbortAsync(name, TestCredentials.Root);
			var result = await _fixture.Client.GetStatusAsync(name, TestCredentials.Root);
			Assert.Contains(result.Status, new[] {"Aborted/Stopped", "Aborted", "Stopped"});
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
