using EventStore.Client;

namespace Kurrent.Client.Tests.Streams.Serialization;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class SerializationTests : KurrentPermanentTests<KurrentPermanentFixture> {
	public SerializationTests(ITestOutputHelper output, KurrentPermanentFixture fixture) : base(output, fixture) {
		Fixture.ClientSettings.Serialization = KurrentClientSerializationSettings.Default();
	}

	[RetryFact]
	public async Task appends_and_reads_with_default_serialization() {
		var stream = $"{Fixture.GetStreamName()}_{StreamState.Any}";

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.Any,
			GenerateEvents()
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 2).ToListAsync();
		Assert.Single(resolvedEvents);

		var resolvedEvent = resolvedEvents.Single();

		Assert.True(resolvedEvent.TryDeserialize(out var message));
		Assert.NotNull(message);
	}

	private IEnumerable<UserRegistered> GenerateEvents(int count = 1) =>
		Enumerable.Range(0, count)
			.Select(
				_ => new UserRegistered(
					Guid.NewGuid(),
					new Address(Guid.NewGuid().ToString(), Guid.NewGuid().GetHashCode())
				)
			);

	public record Address(string Street, int Number);

	public record UserRegistered(Guid UserId, Address Address);
}
