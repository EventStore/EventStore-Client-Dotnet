using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class listing_users : IClassFixture<listing_users.Fixture> {
		private readonly Fixture _fixture;

		public listing_users(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task returns_all_users() {
			var users = await _fixture.Client.ListAllAsync(TestCredentials.Root)
				.ToArrayAsync();

			var expected = new[] {
					new UserDetails("admin", "Event Store Administrator", new[] {"$admins"}, false, default),
					new UserDetails("ops", "Event Store Operations", new[] {"$ops"}, false, default)
				}.Concat(Array.ConvertAll(_fixture.Users, user => new UserDetails(
					user.LoginName,
					user.FullName,
					user.Groups,
					user.Disabled,
					default)))
				.OrderBy(user => user.LoginName)
				.ToArray();

			var actual = Array.ConvertAll(users, user => new UserDetails(
					user.LoginName,
					user.FullName,
					user.Groups,
					user.Disabled,
					default))
				.OrderBy(user => user.LoginName)
				.ToArray();

			Assert.Equal(expected, actual);
		}

		public class Fixture : EventStoreClientFixture {
			public UserDetails[] Users { get; }

			public Fixture() {
				Users = Enumerable.Range(0, 3)
					.Select(_ => new UserDetails(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] {
						Guid.NewGuid().ToString(),
						Guid.NewGuid().ToString()
					}, false, default))
					.ToArray();
			}

			protected override async Task Given() {
				foreach (var user in Users) {
					await Client.CreateUserAsync(user.LoginName, user.FullName,
						user.Groups, Guid.NewGuid().ToString(), TestCredentials.Root);
				}
			}

			protected override Task When() => Task.CompletedTask;
		}
	}
}
