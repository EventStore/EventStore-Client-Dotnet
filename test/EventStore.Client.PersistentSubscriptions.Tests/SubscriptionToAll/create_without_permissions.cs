using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class create_without_permissions
		: IClassFixture<create_without_permissions.Fixture> {
		public create_without_permissions(Fixture fixture) {
			_fixture = fixture;
		}


		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			public Fixture () : base(noDefaultCredentials: true){
			}
			
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}

		[SupportsPSToAll.Fact]
		public Task the_completion_fails_with_access_denied() =>
			Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.Client.CreateToAllAsync("group57",
					new PersistentSubscriptionSettings()));
	}
}
