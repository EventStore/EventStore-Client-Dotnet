using EventStore.Client;

namespace Kurrent.Client.Tests.Streams.Serialization;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class SerializationTests(ITestOutputHelper output, SerializationTests.CustomSerializationFixture fixture)
	: KurrentPermanentTests<SerializationTests.CustomSerializationFixture>(output, fixture) {
	[RetryFact]
	public async Task appends_with_revision_serializes_using_default_json_serialization() {
		var stream = $"{Fixture.GetStreamName()}_{0}";

		var events = GenerateEvents();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, events);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		Assert.Single(resolvedEvents);

		var resolvedEvent = resolvedEvents.Single();

		Assert.NotNull(resolvedEvent.DeserializedEvent);
		Assert.Equal(events.First(), resolvedEvent.DeserializedEvent);
	}

	List<UserRegistered> GenerateEvents(int count = 1) =>
		Enumerable.Range(0, count)
			.Select(
				_ => new UserRegistered(
					Guid.NewGuid(),
					new Address(Guid.NewGuid().ToString(), Guid.NewGuid().GetHashCode())
				)
			).ToList();

	public record Address(string Street, int Number);

	public record UserRegistered(Guid UserId, Address Address);

	public class CustomSerializationFixture() : KurrentPermanentFixture(
		x => {
			x.ClientSettings.Serialization = KurrentClientSerializationSettings
				.Default()
				.EnableAutomaticDeserialization();

			return x;
		}
	);
}
