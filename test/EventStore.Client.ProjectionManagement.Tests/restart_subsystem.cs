namespace EventStore.Client.ProjectionManagement.Tests;

public class restart_subsystem : IClassFixture<restart_subsystem.Fixture> {
	readonly Fixture _fixture;

	public restart_subsystem(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task does_not_throw() => await _fixture.Client.RestartSubsystemAsync(userCredentials: TestCredentials.Root);

	[Fact]
	public async Task throws_when_given_no_credentials() => await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.RestartSubsystemAsync());

	public class Fixture : EventStoreClientFixture {
		public Fixture() : base(noDefaultCredentials: true) { }

		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}