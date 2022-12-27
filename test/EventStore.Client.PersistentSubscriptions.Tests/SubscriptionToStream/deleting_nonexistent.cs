using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class deleting_nonexistent
		: IClassFixture<deleting_nonexistent.Fixture> {
		private readonly Fixture _fixture;
		private const string Stream = nameof(deleting_nonexistent);

		public deleting_nonexistent(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_delete_fails_with_argument_exception() {
			await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
				() => _fixture.Client.DeleteToStreamAsync(Stream,
					Guid.NewGuid().ToString(), userCredentials: TestCredentials.Root));
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
