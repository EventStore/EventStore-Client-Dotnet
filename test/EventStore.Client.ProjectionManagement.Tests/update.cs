namespace EventStore.Client.ProjectionManagement.Tests;

public class @update : IClassFixture<update.Fixture> {
	readonly Fixture _fixture;

	public update(Fixture fixture) => _fixture = fixture;

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task returns_expected_result(bool? emitEnabled) =>
		await _fixture.Client.UpdateAsync(
			nameof(update),
			"fromAll().when({$init: function (s, e) {return {};}});",
			emitEnabled,
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() =>
			Client.CreateContinuousAsync(
				nameof(update),
				"fromAll().when({$init: function (state, ev) {return {};}});",
				userCredentials: TestCredentials.Root
			);

		protected override Task When() => Task.CompletedTask;
	}
}