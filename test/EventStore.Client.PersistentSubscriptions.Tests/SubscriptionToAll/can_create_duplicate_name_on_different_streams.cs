using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class can_create_duplicate_name_on_different_streams
		: IClassFixture<can_create_duplicate_name_on_different_streams.Fixture> {
		public can_create_duplicate_name_on_different_streams(Fixture fixture) {
			_fixture = fixture;
		}



		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override Task When() =>
				Client.CreateToAllAsync("group3211",
					new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
		}

		[SupportsPSToAll.Fact]
		public Task the_completion_succeeds() =>
			_fixture.Client.CreateAsync("someother",
				"group3211", new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
	}
}
