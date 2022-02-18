using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class create_after_deleting_the_same
		: IClassFixture<create_after_deleting_the_same.Fixture> {
		public create_after_deleting_the_same(Fixture fixture) {
			_fixture = fixture;
		}

		private const string Stream = nameof(create_after_deleting_the_same);
		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override async Task When() {
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, CreateTestEvents());
				await Client.CreateAsync(Stream, "existing",
					new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
				await Client.DeleteAsync(Stream, "existing",
					userCredentials: TestCredentials.Root);
			}
		}

		[Fact]
		public async Task the_completion_succeeds() {
			await _fixture.Client.CreateAsync(Stream, "existing",
				new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
		}
	}
}
