namespace EventStore.Client.ProjectionManagement.Tests;

public class @enable : IClassFixture<enable.Fixture> {
	readonly Fixture _fixture;

	public enable(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task status_is_running() {
		var name = StandardProjections.Names.First();
		await _fixture.Client.EnableAsync(name, userCredentials: TestCredentials.Root);
		var result = await _fixture.Client.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Equal("Running", result!.Status);
	}

	public class Fixture : EventStoreClientFixture {
		protected override bool RunStandardProjections => false;
		protected override Task Given()                => Task.CompletedTask;
		protected override Task When()                 => Task.CompletedTask;
	}
}