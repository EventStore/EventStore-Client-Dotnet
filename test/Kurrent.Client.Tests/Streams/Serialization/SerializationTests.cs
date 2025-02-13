using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using EventStore.Client;
using EventStore.Client.Serialization;
using Kurrent.Client.Core.Serialization;
using Kurrent.Diagnostics.Tracing;

namespace Kurrent.Client.Tests.Streams.Serialization;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class SerializationTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task plain_clr_objects_are_serialized_and_deserialized_using_auto_serialization() {
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
		message_data_and_metadata_are_serialized_and_deserialized_using_auto_serialization_with_registered_metadata() {
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
	public async Task
		message_metadata_is_serialized_fully_byt_deserialized_to_tracing_metadata_using_auto_serialization_WITHOUT_registered_custom_metadata() {
		var stream   = Fixture.GetStreamName();
		var metadata = new CustomMetadata(Guid.NewGuid());
		var expected = GenerateMessages();
		List<Message> messagesWithMetadata =
			expected.Select(message => Message.From(message, metadata, Uuid.NewUuid())).ToList();

		// When
		await Fixture.Streams.AppendToStreamAsync(stream, messagesWithMetadata);

		// Then
		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(stream).ToListAsync();
		var messages       = AssertThatMessages(AreDeserialized, expected, resolvedEvents);

		Assert.Equal(messagesWithMetadata.Select(m => m with { Metadata = new TracingMetadata() }), messages);
	}

	[RetryFact]
	public async Task read_stream_without_options_does_NOT_deserialize_resolved_message() {
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

	public static TheoryData<Action<KurrentClientSerializationSettings, string>> CustomTypeMappings() {
		return [
			(settings, typeName) =>
				settings.RegisterMessageType<UserRegistered>(typeName),
			(settings, typeName) =>
				settings.RegisterMessageType(typeof(UserRegistered), typeName),
			(settings, typeName) =>
				settings.RegisterMessageTypes(new Dictionary<Type, string> { { typeof(UserRegistered), typeName } })
		];
	}

	[RetryTheory]
	[MemberData(nameof(CustomTypeMappings))]
	public async Task append_and_read_stream_uses_custom_type_mappings(
		Action<KurrentClientSerializationSettings, string> customTypeMapping
	) {
		// Given
		await using var client = NewClientWith(serialization => customTypeMapping(serialization, "user_registered"));

		// When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		// Then
		var resolvedEvents = await client.ReadStreamAsync(stream).ToListAsync();
		Assert.All(resolvedEvents, resolvedEvent => Assert.Equal("user_registered", resolvedEvent.Event.EventType));

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryTheory]
	[MemberData(nameof(CustomTypeMappings))]
	public async Task append_and_read_all_uses_custom_type_mappings(
		Action<KurrentClientSerializationSettings, string> customTypeMapping
	) {
		// Given
		await using var client = NewClientWith(serialization => customTypeMapping(serialization, "user_registered"));

		// When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		// Then
		var resolvedEvents = await client
			.ReadAllAsync(new ReadAllOptions { Filter = StreamFilter.Prefix(stream) })
			.ToListAsync();

		Assert.All(resolvedEvents, resolvedEvent => Assert.Equal("user_registered", resolvedEvent.Event.EventType));

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task automatic_serialization_custom_json_settings_are_applied() {
		// Given
		var systemTextJsonOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
		};

		await using var client = NewClientWith(serialization => serialization.UseJsonSettings(systemTextJsonOptions));

		// When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		// Then
		var resolvedEvents = await client.ReadStreamAsync(stream).ToListAsync();
		var jsons          = resolvedEvents.Select(e => JsonDocument.Parse(e.Event.Data).RootElement).ToList();

		Assert.Equal(expected.Select(m => m.UserId), jsons.Select(j => j.GetProperty("user-id").GetGuid()));

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	public class CustomMessageTypeNamingStrategy : IMessageTypeNamingStrategy {
		public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) {
			return $"custom-{messageType}";
		}

#if NET48
	public bool TryResolveClrType(string messageTypeName, out Type? type) {
#else
		public bool TryResolveClrType(string messageTypeName, [NotNullWhen(true)] out Type? type) {
#endif
			var typeName = messageTypeName[(messageTypeName.IndexOf('-') + 1)..];
			type = Type.GetType(typeName);

			return type != null;
		}

#if NET48
	public bool TryResolveClrMetadataType(string messageTypeName, out Type? type) {
#else
		public bool TryResolveClrMetadataType(string messageTypeName, [NotNullWhen(true)] out Type? type) {
#endif
			type = null;
			return false;
		}
	}

	[RetryFact]
	public async Task append_and_read_stream_uses_custom_message_type_naming_strategy() {
		// Given
		await using var client = NewClientWith(
			serialization => serialization.UseMessageTypeNamingStrategy<CustomMessageTypeNamingStrategy>()
		);

		//When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		//Then
		var resolvedEvents = await Fixture.Streams.ReadStreamAsync(stream).ToListAsync();
		Assert.All(resolvedEvents, resolvedEvent => Assert.Equal($"custom-{typeof(UserRegistered).FullName}", resolvedEvent.Event.EventType));
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task append_and_read_all_uses_custom_message_type_naming_strategy() {
		// Given
		await using var client = NewClientWith(
			serialization => serialization.UseMessageTypeNamingStrategy<CustomMessageTypeNamingStrategy>()
		);

		//When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		//Then
		var resolvedEvents = await client
			.ReadAllAsync(new ReadAllOptions { Filter = StreamFilter.Prefix(stream) })
			.ToListAsync();

		Assert.All(resolvedEvents, resolvedEvent => Assert.Equal($"custom-{typeof(UserRegistered).FullName}", resolvedEvent.Event.EventType));
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task read_stream_deserializes_resolved_message_appended_with_manual_compatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(
			message => $"stream-{message.GetType().FullName!}"
		);

		// When
		var resolvedEvents = await Fixture.Streams
			.ReadStreamAsync(stream)
			.ToListAsync();

		// Then
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task read_all_deserializes_resolved_message_appended_with_manual_compatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(
			message => $"stream-{message.GetType().FullName!}"
		);

		// When
		var resolvedEvents = await Fixture.Streams
			.ReadAllAsync(new ReadAllOptions { Filter = StreamFilter.Prefix(stream) })
			.ToListAsync();

		// Then
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task read_does_NOT_deserialize_resolved_message_appended_with_manual_incompatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(_ => "user_registered");

		// When
		var resolvedEvents = await Fixture.Streams
			.ReadStreamAsync(stream)
			.ToListAsync();

		// Then
		AssertThatMessages(AreNotDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task read_all_does_NOT_deserialize_resolved_message_appended_with_manual_incompatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(_ => "user_registered");

		// When
		var resolvedEvents = await Fixture.Streams
			.ReadAllAsync(new ReadAllOptions { Filter = StreamFilter.Prefix(stream) })
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

	async Task<(string, List<UserRegistered>)> AppendEventsUsingAutoSerialization(KurrentClient? kurrentClient = null) {
		var stream   = Fixture.GetStreamName();
		var messages = GenerateMessages();

		var writeResult = await (kurrentClient ?? Fixture.Streams).AppendToStreamAsync(stream, messages);
		Assert.Equal(new((ulong)messages.Count - 1), writeResult.NextExpectedStreamRevision);

		return (stream, messages);
	}

	async Task<(string, List<UserRegistered>)> AppendEventsUsingManualSerialization(
		Func<UserRegistered, string> getTypeName
	) {
		var stream   = Fixture.GetStreamName();
		var messages = GenerateMessages();
		var eventData = messages.Select(
			message =>
				new EventData(
					Uuid.NewUuid(),
					getTypeName(message),
					Encoding.UTF8.GetBytes(
						JsonSerializer.Serialize(
							message,
							SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions
						)
					)
				)
		);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamRevision.None, eventData);
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
		settings.Serialization = settings.Serialization.Clone();
		customizeSerialization(settings.Serialization);

		return new KurrentClient(settings);
	}

	public record Address(string Street, int Number);

	public record UserRegistered(Guid UserId, Address Address);

	public record CustomMetadata(Guid UserId);
}
