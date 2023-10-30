using System.Runtime.CompilerServices;
using System.Text;

namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Network")]
[Trait("Category", "LongRunning")]
public class is_json : IClassFixture<is_json.Fixture> {
	readonly Fixture _fixture;

	public is_json(Fixture fixture) => _fixture = fixture;

	public static IEnumerable<object?[]> TestCases() {
		var json = @"{""some"":""json""}";

		yield return new object?[] { true, json, string.Empty };
		yield return new object?[] { true, string.Empty, json };
		yield return new object?[] { true, json, json };
		yield return new object?[] { false, json, string.Empty };
		yield return new object?[] { false, string.Empty, json };
		yield return new object?[] { false, json, json };
	}

	[Theory]
	[MemberData(nameof(TestCases))]
	public async Task is_preserved(bool isJson, string data, string metadata) {
		var stream   = GetStreamName(isJson, data, metadata);
		var encoding = Encoding.UTF8;
		var eventData = new EventData(
			Uuid.NewUuid(),
			"-",
			encoding.GetBytes(data),
			encoding.GetBytes(metadata),
			isJson
				? Constants.Metadata.ContentTypes.ApplicationJson
				: Constants.Metadata.ContentTypes.ApplicationOctetStream
		);

		await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, new[] { eventData });

		var @event = await _fixture.Client
			.ReadStreamAsync(
				Direction.Forwards,
				stream,
				StreamPosition.Start,
				1,
				true
			)
			.FirstOrDefaultAsync();

		Assert.Equal(
			isJson
				? Constants.Metadata.ContentTypes.ApplicationJson
				: Constants.Metadata.ContentTypes.ApplicationOctetStream,
			@event.Event.ContentType
		);

		Assert.Equal(data, encoding.GetString(@event.Event.Data.ToArray()));
		Assert.Equal(metadata, encoding.GetString(@event.Event.Metadata.ToArray()));
	}

	string GetStreamName(bool isJson, string data, string metadata, [CallerMemberName] string? testMethod = default) =>
		$"{_fixture.GetStreamName(testMethod)}_{isJson}_{(data == string.Empty ? "no_data" : "data")}_{(metadata == string.Empty ? "no_metadata" : "metadata")}";

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}