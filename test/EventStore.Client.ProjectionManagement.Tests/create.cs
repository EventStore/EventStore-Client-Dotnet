using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class @create : IClassFixture<create.Fixture> {
		private readonly Fixture _fixture;

		public create(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task one_time() {
			await _fixture.Client.CreateOneTimeAsync(
				"fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);
		}

		[Theory, InlineData(true), InlineData(false)]
		public async Task continuous(bool trackEmittedStreams) {
			await _fixture.Client.CreateContinuousAsync(
				$"{nameof(continuous)}_{trackEmittedStreams}",
				"fromAll().when({$init: function (state, ev) {return {};}});", trackEmittedStreams,
				userCredentials: TestCredentials.Root);
		}

		[Fact]
		public async Task transient() {
			await _fixture.Client.CreateTransientAsync(
				nameof(transient),
				"fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
