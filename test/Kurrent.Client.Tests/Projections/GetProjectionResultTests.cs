using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.Projections;

[Trait("Category", "Target:Projections")]
public class GetProjectionResultTests(ITestOutputHelper output, GetProjectionResultTests.CustomFixture fixture)
	: KurrentTemporaryTests<GetProjectionResultTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task get_result() {
		var     name   = Fixture.GetProjectionName();
		Result? result = null;

		var projection = $$"""
		                   fromStream('{{name}}').when({
		                   	"$init": function() { return { Count: 0 }; },
		                   	"$any": function(s, e) { s.Count++; return s; }
		                   });
		                   """;

		await Fixture.Projections.CreateContinuousAsync(
			name,
			projection,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(
			name,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		await AssertEx.IsOrBecomesTrue(
			async () => {
				result = await Fixture.Projections.GetResultAsync<Result>(name, userCredentials: TestCredentials.Root);
				return result.Count > 0;
			}
		);

		Assert.NotNull(result);
		Assert.Equal(1, result!.Count);
	}

	record Result {
		public int Count { get; set; }
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
