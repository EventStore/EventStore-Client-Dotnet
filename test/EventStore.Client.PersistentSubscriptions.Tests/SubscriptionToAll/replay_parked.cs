using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class replay_parked : IClassFixture<replay_parked.Fixture> {
		private readonly Fixture _fixture;
		private const string GroupName = nameof(replay_parked);

		public replay_parked(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task throws_when_not_supported() {
			if (SupportsPSToAll.No) {
				
				await Assert.ThrowsAsync<NotSupportedException>(async () => {
					await _fixture.Client.ReplayParkedMessagesToAllAsync(
						GroupName,
						userCredentials: TestCredentials.Root);
				});
			}
		}
		
		[SupportsPSToAll.Fact]
		public async Task does_not_throw() {
			await _fixture.Client.ReplayParkedMessagesToAllAsync(
				GroupName,
				userCredentials: TestCredentials.Root);
			
			await _fixture.Client.ReplayParkedMessagesToAllAsync(
				GroupName,
				stopAt: 100,
				userCredentials: TestCredentials.Root);
		}

		[SupportsPSToAll.Fact]
		public async Task throws_when_given_non_existing_subscription() {
			await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(() => 
				_fixture.Client.ReplayParkedMessagesToAllAsync(
					groupName: "NonExisting",
					userCredentials: TestCredentials.Root));
		}
		
		[SupportsPSToAll.Fact]
		public async Task throws_with_no_credentials() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => 
				_fixture.Client.ReplayParkedMessagesToAllAsync(GroupName));
		}

		[SupportsPSToAll.Fact]
		public async Task throws_with_non_existing_user() {
			await Assert.ThrowsAsync<NotAuthenticatedException>(() => 
				_fixture.Client.ReplayParkedMessagesToAllAsync(
					GroupName,
					userCredentials: TestCredentials.TestBadUser));
		}
		
		[SupportsPSToAll.Fact]
		public async Task throws_with_normal_user_credentials() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => 
				_fixture.Client.ReplayParkedMessagesToAllAsync(
					GroupName,
					userCredentials: TestCredentials.TestUser1));
		}

		public class Fixture : EventStoreClientFixture {
			public Fixture () : base(noDefaultCredentials: true) {
			}
			
			protected override async Task Given() {
				if (SupportsPSToAll.No) {
					return;
				}
				
				await Client.CreateToAllAsync(
					GroupName,
					new PersistentSubscriptionSettings(),
					userCredentials: TestCredentials.Root);
			}
				
			protected override Task When() => Task.CompletedTask;
		}
	}
}
