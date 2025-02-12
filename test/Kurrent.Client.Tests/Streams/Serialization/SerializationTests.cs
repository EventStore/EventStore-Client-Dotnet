using System.Text.Json;
using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests.Streams.Serialization;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class SerializationTests(ITestOutputHelper output, SerializationTests.CustomSerializationFixture fixture)
	: KurrentPermanentTests<SerializationTests.CustomSerializationFixture>(output, fixture) {
	[RetryFact]
	public async Task appends_with_raw_messages_serializes_and_deserializes_using_default_json_serialization() {
		// Given
		var                  stream   = Fixture.GetStreamName();
		List<UserRegistered> expected = GenerateMessages();

		//When
		await Fixture.Streams.AppendToStreamAsync(stream, expected);

		//Then
		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(stream).ToListAsync();
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task
		appends_with_messages_serializes_and_deserializes_data_and_metadata_using_default_json_serialization() {
		// Given
		await using var client = NewClientWith(serialization => serialization.UseMetadataType<CustomMetadata>());

		var stream   = Fixture.GetStreamName();
		var metadata = new CustomMetadata(Guid.NewGuid());
		var expected = GenerateMessages();
		List<Message> messagesWithMetadata =
			expected.Select(message => Message.From(message, metadata, Uuid.NewUuid())).ToList();

		// When
		await client.AppendToStreamAsync(stream, messagesWithMetadata);

		// Then
		var resolvedEvents = await client.ReadStreamAsync(stream).ToListAsync();
		var messages       = AssertThatMessages(AreDeserialized, expected, resolvedEvents);

		Assert.Equal(messagesWithMetadata, messages);
	}

	[RetryFact]
	public async Task read_without_options_does_NOT_deserialize_resolved_message() {
		// Given
		var (stream, expected) = await AppendEventsUsingAutoSerialization();

		// When
		var resolvedEvents = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		// Then
		AssertThatMessages(AreNotDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task read_all_without_options_does_NOT_deserialize_resolved_message() {
		// Given
		var (stream, expected) = await AppendEventsUsingAutoSerialization();

		// When
		var resolvedEvents = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, StreamFilter.Prefix(stream))
			.ToListAsync();

		// Then
		AssertThatMessages(AreNotDeserialized, expected, resolvedEvents);
	}

	static List<Message> AssertThatMessages(
		Action<UserRegistered, ResolvedEvent> assertMatches,
		List<UserRegistered> expected,
		List<ResolvedEvent> resolvedEvents
	) {
		Assert.Equal(expected.Count, resolvedEvents.Count);
		Assert.NotEmpty(resolvedEvents);

		Assert.All(resolvedEvents, (resolvedEvent, idx) => assertMatches(expected[idx], resolvedEvent));

		return resolvedEvents.Select(resolvedEvent => resolvedEvent.Message!).ToList();
	}

	static void AreDeserialized(UserRegistered expected, ResolvedEvent resolvedEvent) {
		Assert.NotNull(resolvedEvent.Message);
		Assert.Equal(expected, resolvedEvent.Message.Data);
		Assert.Equal(expected, resolvedEvent.DeserializedData);
	}

	static void AreNotDeserialized(UserRegistered expected, ResolvedEvent resolvedEvent) {
		Assert.Null(resolvedEvent.Message);
		Assert.Equal(
			expected,
			JsonSerializer.Deserialize<UserRegistered>(
				resolvedEvent.Event.Data.Span,
				SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions
			)
		);
	}

	async Task<(string, List<UserRegistered>)> AppendEventsUsingAutoSerialization() {
		var stream   = Fixture.GetStreamName();
		var messages = GenerateMessages();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messages);
		Assert.Equal(new((ulong)messages.Count - 1), writeResult.NextExpectedStreamRevision);

		return (stream, messages);
	}

	static List<UserRegistered> GenerateMessages(int count = 2) =>
		Enumerable.Range(0, count)
			.Select(
				_ => new UserRegistered(
					Guid.NewGuid(),
					new Address(Guid.NewGuid().ToString(), Guid.NewGuid().GetHashCode())
				)
			).ToList();

	KurrentClient NewClientWith(Action<KurrentClientSerializationSettings> customizeSerialization) {
		var settings = Fixture.ClientSettings;
		customizeSerialization(settings.Serialization);

		return new KurrentClient(settings);
	}

	public record Address(string Street, int Number);

	public record UserRegistered(Guid UserId, Address Address);

	public record CustomMetadata(Guid UserId);

	public class CustomSerializationFixture() : KurrentPermanentFixture(
		x => {
			x.ClientSettings.Serialization = KurrentClientSerializationSettings.Default();

			return x;
		}
	);
}
