using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class get_state : IClassFixture<get_state.Fixture> {
		private readonly Fixture _fixture;

		public get_state(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task returns_expected_result() {
			var result =
				await _fixture.Client.GetStateAsync<Result>(nameof(get_state),
					userCredentials: TestCredentials.TestUser1);
			Assert.Equal(1, result.Count);
		}

		private class Result {
			public int Count { get; set; }
		}

		public class Fixture : EventStoreClientFixture {
			private static readonly string Projection = $@"
fromStream('{nameof(get_state)}').when({{
	""$init"": function() {{ return {{ Count: 0 }}; }},
	""$any"": function(s, e) {{ s.Count++; return s; }}
}});
";

			protected override Task Given() => Client.CreateContinuousAsync(nameof(get_state),
				Projection, userCredentials: TestCredentials.Root);

			protected override Task When() => StreamsClient.AppendToStreamAsync(nameof(get_state), StreamState.NoStream,
				CreateTestEvents());
		}
	}
}
