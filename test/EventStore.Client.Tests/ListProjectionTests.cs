// ReSharper disable InconsistentNaming

using EventStore.Client.Tests.TestNode;

namespace EventStore.Client.Tests;

public class ListProjectionTests(ITestOutputHelper output, ListProjectionTests.CustomFixture fixture)
	: KurrentTemporaryTests<ListProjectionTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task list_all_projections() {
		var result = await Fixture.Projections.ListAllAsync(userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		Assert.Equal(result.Select(x => x.Name).OrderBy(x => x), Names.OrderBy(x => x));
	}

	[Fact]
	public async Task list_continuous_projections() {
		var name = Fixture.GetProjectionName();

		await Fixture.Projections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);

		var result = await Fixture.Projections.ListContinuousAsync(userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		Assert.Equal(
			result.Select(x => x.Name).OrderBy(x => x),
			Names.Concat([name]).OrderBy(x => x)
		);

		Assert.True(result.All(x => x.Mode == "Continuous"));
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
