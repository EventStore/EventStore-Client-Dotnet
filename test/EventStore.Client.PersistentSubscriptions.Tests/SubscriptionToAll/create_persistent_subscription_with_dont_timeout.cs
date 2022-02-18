using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class create_with_dont_timeout
		: IClassFixture<create_with_dont_timeout.Fixture> {
		public create_with_dont_timeout(Fixture fixture) {
			_fixture = fixture;
		}


		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}

		[Fact]
		public Task the_subscription_is_created_without_error() =>
			_fixture.Client.CreateToAllAsync("dont-timeout",
				new PersistentSubscriptionSettings(messageTimeout: TimeSpan.Zero),
				userCredentials: TestCredentials.Root);
	}
}
