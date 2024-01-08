namespace EventStore.Client.ProjectionManagement.Tests;

public class @abort : IClassFixture<abort.Fixture> {
	readonly Fixture _fixture;

	public abort(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task status_is_aborted() {
		var name = StandardProjections.Names.First();
		await _fixture.Client.AbortAsync(name, userCredentials: TestCredentials.Root);
		var result = await _fixture.Client.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Contains(new[] { "Aborted/Stopped", "Stopped" }, x => x == result!.Status);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}