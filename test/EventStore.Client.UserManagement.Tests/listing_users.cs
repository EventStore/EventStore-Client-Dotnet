using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Tests.Fixtures;
using Xunit;

namespace EventStore.Client {
	public class listing_users : IClassFixture<EventStoreUserManagementFixture> {
		readonly EventStoreUserManagementFixture _fixture;

		public listing_users(EventStoreUserManagementFixture fixture) => _fixture = fixture;

		[Fact]
		public async Task returns_all_users() {
			var seedUsers = Enumerable.Range(0, 3).Select(_ => new UserDetails(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] {
				Guid.NewGuid().ToString(),
				Guid.NewGuid().ToString()
			}, false, default)).ToArray();

			foreach (var user in seedUsers) {
				await _fixture.Client.CreateUserAsync(user.LoginName, user.FullName, user.Groups, Guid.NewGuid().ToString(),
					userCredentials: TestCredentials.Root);
			}

			var users = await _fixture.Client.ListAllAsync(userCredentials: TestCredentials.Root).ToArrayAsync();

			var expected = new[] {
					new UserDetails("admin", "Event Store Administrator", new[] { "$admins" }, false, default),
					new UserDetails("ops", "Event Store Operations", new[] { "$ops" }, false, default)
				}.Concat(Array.ConvertAll(seedUsers, user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default)))
				.OrderBy(user => user.LoginName).ToArray();

			var actual = Array.ConvertAll(users, user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default))
				.OrderBy(user => user.LoginName).ToArray();

			Assert.Equal(expected, actual);
		}
	}
}
