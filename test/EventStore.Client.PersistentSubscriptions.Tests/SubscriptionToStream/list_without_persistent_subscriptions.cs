using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream;

public class list_without_persistent_subscriptions : IClassFixture<list_without_persistent_subscriptions.Fixture> {
	private readonly Fixture _fixture;
		
	public list_without_persistent_subscriptions(Fixture fixture) {
		_fixture = fixture;
	}

	[SupportsPSToAll.Fact]
	public async Task throws() {
		if (SupportsPSToAll.No) {
			return;
		}
		
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(async () => 
			await _fixture.Client.ListToStreamAsync("stream", userCredentials: TestCredentials.Root));
	}
	
	[Fact]
	public async Task returns_empty_collection() {
		if (SupportsPSToAll.No) {
			return;
		}

		var result = await _fixture.Client.ListAllAsync(userCredentials: TestCredentials.Root);
		
		Assert.Empty(result);
	}
	
	public class Fixture : EventStoreClientFixture {
		public Fixture () : base(skipPSWarmUp: true) {
		}
		
		protected override Task Given() => Task.CompletedTask;

		protected override Task When() => Task.CompletedTask;
	}
}
