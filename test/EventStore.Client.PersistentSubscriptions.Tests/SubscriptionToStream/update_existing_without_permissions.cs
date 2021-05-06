using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class update_existing_without_permissions
		: IClassFixture<update_existing_without_permissions.Fixture> {
		private const string Stream = nameof(update_existing_without_permissions);
		private const string Group = "existing";
		private readonly Fixture _fixture;

		public update_existing_without_permissions(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_completion_fails_with_access_denied() {
			await Assert.ThrowsAsync<AccessDeniedException>(
				() => _fixture.Client.UpdateAsync(Stream, Group,
					new PersistentSubscriptionSettings()));
		}

		public class Fixture : EventStoreClientFixture {
			protected override async Task Given() {
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, CreateTestEvents());
				await Client.CreateAsync(Stream, Group, new PersistentSubscriptionSettings(),
					TestCredentials.Root);
			}

			protected override Task When() => Task.CompletedTask;
		}
	}
}
