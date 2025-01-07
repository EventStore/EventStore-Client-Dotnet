// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests;

public class ProjectionManagementTests(ITestOutputHelper output, ProjectionManagementTests.CustomFixture fixture)
	: KurrentTemporaryTests<ProjectionManagementTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task status_is_aborted() {
		var name = Names.First();
		await Fixture.Projections.AbortAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.Projections.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Contains(["Aborted/Stopped", "Stopped"], x => x == result.Status);
	}

	[Fact]
	public async Task one_time() =>
		await Fixture.Projections.CreateOneTimeAsync("fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task continuous(bool trackEmittedStreams) {
		var name = Fixture.GetProjectionName();

		await Fixture.Projections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			trackEmittedStreams,
			userCredentials: TestCredentials.Root
		);
	}

	[Fact]
	public async Task transient() {
		var name = Fixture.GetProjectionName();

		await Fixture.Projections.CreateTransientAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
