namespace EventStore.Client; 

public class get_result : IClassFixture<get_result.Fixture> {
    private readonly Fixture _fixture;

    public get_result(Fixture fixture) {
        _fixture = fixture;
    }

    [Fact]
    public async Task returns_expected_result() {
        Result? result = null;

        await AssertEx.IsOrBecomesTrue(async () => {
            result = await _fixture.Client
                .GetResultAsync<Result>(nameof(get_result), userCredentials: TestCredentials.TestUser1);

            return result.Count > 0;
        });

        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
    }

    private class Result {
        public int Count { get; set; }
    }

    public class Fixture : EventStoreClientFixture {
        private static readonly string Projection = $@"
fromStream('{nameof(get_result)}').when({{
	""$init"": function() {{ return {{ Count: 0 }}; }},
	""$any"": function(s, e) {{ s.Count++; return s; }}
}});
";

        protected override Task Given() => Client.CreateContinuousAsync(nameof(get_result),
                                                                        Projection, userCredentials: TestCredentials.Root);

        protected override async Task When() {
            await StreamsClient.AppendToStreamAsync(nameof(get_result), StreamState.NoStream,
                                                    CreateTestEvents());
        }
    }
}