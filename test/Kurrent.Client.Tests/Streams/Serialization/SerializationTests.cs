using EventStore.Client;

namespace Kurrent.Client.Tests.Streams.Serialization;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class SerializationTests : KurrentPermanentTests<KurrentPermanentFixture> {
	public SerializationTests(ITestOutputHelper output, KurrentPermanentFixture fixture) : base(output, fixture) {
		Fixture.ClientSettings.Serialization = KurrentClientSerializationSettings.Default();
	}
	
	[RetryFact]
	public async Task appends_with_revision_serializes_using_default_json_serialization() {
		var stream = $"{Fixture.GetStreamName()}_{StreamState.Any}";

		var events = GenerateEvents();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamRevision.None, 
			events
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 2).ToListAsync();
		Assert.Single(resolvedEvents);

		var resolvedEvent = resolvedEvents.Single();

		Assert.True(resolvedEvent.TryDeserialize(out var message));
		Assert.Equal(events.First(), message);
	}
	

	[RetryFact]
	public async Task appends_with_stream_state_serializes_using_default_json_serialization() {
		var stream = $"{Fixture.GetStreamName()}_{StreamState.Any}";

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.Any,
			GenerateEvents()
		);
		
		var events = GenerateEvents();

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 2).ToListAsync();
		Assert.Single(resolvedEvents);

		var resolvedEvent = resolvedEvents.Single();

		Assert.True(resolvedEvent.TryDeserialize(out var message));
		Assert.NotNull(message);
		Assert.Equal(events.First(), message);
	}

	private List<UserRegistered> GenerateEvents(int count = 1) =>
		Enumerable.Range(0, count)
			.Select(
				_ => new UserRegistered(
					Guid.NewGuid(),
					new Address(Guid.NewGuid().ToString(), Guid.NewGuid().GetHashCode())
				)
			).ToList();

	public record Address(string Street, int Number);

	public record UserRegistered(Guid UserId, Address Address);
}
