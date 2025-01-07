using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.Projections;

public class ListOneTimeProjectionsTests(ITestOutputHelper output, ListOneTimeProjectionsTests.CustomFixture fixture)
	: KurrentTemporaryTests<ListOneTimeProjectionsTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task list_one_time_projections() {
		await Fixture.Projections.CreateOneTimeAsync("fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);

		var result = await Fixture.Projections.ListOneTimeAsync(userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		var details = Assert.Single(result);
		Assert.Equal("OneTime", details.Mode);
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
