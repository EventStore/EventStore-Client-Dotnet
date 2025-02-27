using System.Diagnostics.CodeAnalysis;
using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests.Core.Serialization;

public class MessageSerializerExtensionsTests {
	[Fact]
	public void With_NullOperationSettings_ReturnsDefaultSerializer() {
		// Given
		var defaultSerializer = new DummyMessageSerializer();
		var defaultSettings   = new KurrentClientSerializationSettings();

		// When
		var result = defaultSerializer.With(defaultSettings, null);

		// Then
		Assert.Same(defaultSerializer, result);
	}

	[Fact]
	public void With_DisabledAutomaticDeserialization_ReturnsNullSerializer() {
		// Given
		var defaultSerializer = new DummyMessageSerializer();
		var defaultSettings   = new KurrentClientSerializationSettings();
		var operationSettings = OperationSerializationSettings.Disabled;

		// When
		var result = defaultSerializer.With(defaultSettings, operationSettings);

		// Then
		Assert.Same(NullMessageSerializer.Instance, result);
	}

	[Fact]
	public void With_NoConfigureSettings_ReturnsDefaultSerializer() {
		// Given
		var defaultSerializer = new DummyMessageSerializer();
		var defaultSettings   = new KurrentClientSerializationSettings();
		var operationSettings = new OperationSerializationSettings(); // Default-enabled with no config

		// When
		var result = defaultSerializer.With(defaultSettings, operationSettings);

		// Then
		Assert.Same(defaultSerializer, result);
	}

	[Fact]
	public void With_ConfigureSettings_CreatesNewMessageSerializer() {
		// Given
		var defaultSerializer = new DummyMessageSerializer();
		var defaultSettings = KurrentClientSerializationSettings.Default();

		var operationSettings = OperationSerializationSettings.Configure(
			s =>
				s.RegisterMessageType<MessageSerializerTests.UserRegistered>("CustomMessageName")
		);

		// When
		var result = defaultSerializer.With(defaultSettings, operationSettings);

		// Then
		Assert.NotSame(defaultSerializer, result);
		Assert.IsType<MessageSerializer>(result);
	}

	[Fact]
	public void Serialize_WithMultipleMessages_ReturnsArrayOfEventData() {
		// Given
		var serializer = new DummyMessageSerializer();
		var messages = new List<Message> {
			Message.From(new object()),
			Message.From(new object()),
			Message.From(new object())
		};

		var context = new MessageSerializationContext("test-stream", ContentType.Json);

		// When
		var result = serializer.Serialize(messages, context);

		// Then
		Assert.Equal(3, result.Length);
		Assert.All(result, eventData => Assert.Equal("TestEvent", eventData.Type));
	}

	class DummyMessageSerializer : IMessageSerializer {
		public EventData Serialize(Message value, MessageSerializationContext context) {
			return new EventData(
				Uuid.NewUuid(),
				"TestEvent",
				ReadOnlyMemory<byte>.Empty,
				ReadOnlyMemory<byte>.Empty,
				"application/json"
			);
		}

#if NET48
	public bool TryDeserialize(EventRecord record, out Message? deserialized) {
#else
		public bool TryDeserialize(EventRecord record, [NotNullWhen(true)] out Message? deserialized) {
#endif
			deserialized = null;
			return false;
		}
	}
	
	public record UserRegistered(string UserId, string Email);
}
