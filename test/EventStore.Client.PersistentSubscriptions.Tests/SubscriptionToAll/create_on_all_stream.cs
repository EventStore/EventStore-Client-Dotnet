using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class create_on_all_stream
		: IClassFixture<create_on_all_stream.Fixture> {
		public create_on_all_stream(Fixture fixture) {
			_fixture = fixture;
		}

		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}

		[Fact]
		public Task the_completion_succeeds()
			=> _fixture.Client.CreateToAllAsync(
				"existing", new PersistentSubscriptionSettings(), TestCredentials.Root);

		[Fact]
		public Task throws_argument_exception_if_wrong_start_from_type_passed()
			=> Assert.ThrowsAsync<ArgumentException>(() => _fixture.Client.CreateToAllAsync(
				"existing", new PersistentSubscriptionSettings(startFrom: StreamPosition.End), TestCredentials.Root));
	}
}
