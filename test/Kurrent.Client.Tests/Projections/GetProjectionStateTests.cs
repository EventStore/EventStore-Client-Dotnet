using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.Projections;

public class GetProjectionStateTests(ITestOutputHelper output, GetProjectionStateTests.CustomFixture fixture)
	: KurrentTemporaryTests<GetProjectionStateTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task get_state() {
		var name = Fixture.GetProjectionName();

		var projection = $$"""
		                   fromStream('{{name}}').when({
		                   	"$init": function() { return { Count: 0 }; },
		                   	"$any": function(s, e) { s.Count++; return s; }
		                   });
		                   """;

		Result? result = null;

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
				result = await Fixture.Projections.GetStateAsync<Result>(name, userCredentials: TestCredentials.Root);
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
