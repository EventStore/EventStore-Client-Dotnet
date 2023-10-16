using System.Threading.Tasks;
using EventStore.Tests.Fixtures;

namespace EventStore.Client {
	public class getting_current_user : IClassFixture<EventStoreUserManagementFixture> {
		readonly EventStoreUserManagementFixture _fixture;

		public getting_current_user(EventStoreUserManagementFixture fixture) => _fixture = fixture;

		[Fact]
		public async Task returns_the_current_user() {
			var user = await _fixture.Client.GetCurrentUserAsync(TestCredentials.Root);
			Assert.Equal(TestCredentials.Root.Username, user.LoginName);
			user.LoginName.ShouldBe(TestCredentials.Root.Username);
		}
	}
}
