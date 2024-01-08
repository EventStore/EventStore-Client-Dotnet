namespace EventStore.Client.ProjectionManagement.Tests;

public class @disable : IClassFixture<disable.Fixture> {
	readonly Fixture _fixture;

	public disable(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task status_is_stopped() {
		var name = StandardProjections.Names.First();
		await _fixture.Client.DisableAsync(name, userCredentials: TestCredentials.Root);
		var result = await _fixture.Client.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Contains(new[] { "Aborted/Stopped", "Stopped" }, x => x == result!.Status);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}