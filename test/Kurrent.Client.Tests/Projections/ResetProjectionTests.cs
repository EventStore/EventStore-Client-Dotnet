using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.Projections;

[Trait("Category", "Target:Projections")]
public class ResetProjectionTests(ITestOutputHelper output, ResetProjectionTests.CustomFixture fixture)
	: KurrentTemporaryTests<ResetProjectionTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task reset_projection() {
		var name = Names.First();
		await Fixture.Projections.ResetAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.Projections.GetStatusAsync(name, userCredentials: TestCredentials.Root);

		Assert.NotNull(result);
		Assert.Equal("Running", result.Status);
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
