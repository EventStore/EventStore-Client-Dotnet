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
		var stream = $"{Fixture.GetStreamName()}_{0}";

		List<UserRegistered> messages = GenerateMessages();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messages);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(stream)
			.ToListAsync();

		AssertResolvedEventIsAutomaticallyDeserialized(resolvedEvents, messages);
	}
	
	[RetryFact]
	public async Task appends_with_messages_serializes_and_deserializes_data_using_default_json_serialization() {
		var stream   = $"{Fixture.GetStreamName()}_{0}";
		var metadata = new CustomMetadata(Guid.NewGuid());

		var messages = GenerateMessages();
		List<Message> messagesWithMetadata = messages.Select(message => Message.From(message, metadata, Uuid.NewUuid())).ToList();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messagesWithMetadata);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(stream)
			.ToListAsync();

		var resolvedEvent = AssertResolvedEventIsAutomaticallyDeserialized(resolvedEvents, messages);
		
		Assert.Equal(messagesWithMetadata.First().Metadata, JsonSerializer.Deserialize<CustomMetadata>(
			resolvedEvent.Event.Metadata.Span,
			SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions
		));
	}

	[RetryFact]
	public async Task read_without_options_does_NOT_deserialize_resolved_message() {
		var stream = $"{Fixture.GetStreamName()}_{0}";

		var events = await AppendEventsUsingAutoSerialization(stream);

		var resolvedEvents = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		AssertResolvedEventIsNotAutomaticallyDeserialized(resolvedEvents, events);
	}

	[RetryFact]
	public async Task read_all_without_options_does_NOT_deserialize_resolved_message() {
		var stream = $"{Fixture.GetStreamName()}_{0}";

		var events = await AppendEventsUsingAutoSerialization(stream);

		var resolvedEvents = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, StreamFilter.Prefix(stream))
			.ToListAsync();

		AssertResolvedEventIsNotAutomaticallyDeserialized(resolvedEvents, events);
	}
	
	static ResolvedEvent AssertResolvedEventIsAutomaticallyDeserialized(
		List<ResolvedEvent> resolvedEvents, 
		List<UserRegistered> messages
	) {
		Assert.Single(resolvedEvents);

		var resolvedEvent = resolvedEvents.Single();

		Assert.NotNull(resolvedEvent.Message);
		Assert.Equal(messages.First(), resolvedEvent.Message);

		return resolvedEvent;
	}

	static void AssertResolvedEventIsNotAutomaticallyDeserialized(
		List<ResolvedEvent> resolvedEvents, 
		List<UserRegistered> messages
	) {
		Assert.Single(resolvedEvents);

		var resolvedEvent = resolvedEvents.Single();

		Assert.Null(resolvedEvent.Message);
		Assert.Equal(
			messages.First(),
			JsonSerializer.Deserialize<UserRegistered>(
				resolvedEvent.Event.Data.Span,
				SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions
			)
		);
	}

	async Task<List<UserRegistered>> AppendEventsUsingAutoSerialization(string stream) {
		var events = GenerateMessages();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, events);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);
		return events;
	}

	List<UserRegistered> GenerateMessages(int count = 1) =>
		Enumerable.Range(0, count)
			.Select(
				_ => new UserRegistered(
					Guid.NewGuid(),
					new Address(Guid.NewGuid().ToString(), Guid.NewGuid().GetHashCode())
				)
			).ToList();

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
