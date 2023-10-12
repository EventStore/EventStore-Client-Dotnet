using System.Threading.Tasks;

namespace EventStore.Client;

public class SimpleFixture : EventStoreClientFixture {
	public SimpleFixture() : base(noDefaultCredentials: true) { }

	protected override Task Given() => Task.CompletedTask;
	protected override Task When() => Task.CompletedTask;
}
