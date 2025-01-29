using System.Diagnostics.CodeAnalysis;
using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

public record SerializationContext(
	SchemaRegistry SchemaRegistry,
	SerializationType DefaultSerializationType,
	AutomaticDeserialization AutomaticDeserialization
) {
	public EventData[] Serialize(IEnumerable<object> messages) {
		return Serialize(DefaultSerializationType, messages);
	}

	public EventData[] Serialize(SerializationType serializationType, IEnumerable<object> messages) {
		if (AutomaticDeserialization == AutomaticDeserialization.Disabled)
			throw new InvalidOperationException("Cannot serialize, automatic deserialization is disabled");
		
		var serializer = SchemaRegistry.GetSerializer((SchemaDefinitionType)(int)serializationType);

		return messages.Select(
			@event => {
				var (bytes, typeName) = serializer.Serialize(@event);
				return new EventData(Uuid.NewUuid(), typeName, bytes);
			}
		).ToArray();
	}

#if NET48
	public bool TryDeserialize(EventRecord eventRecord, out object? deserialized) {
#else
	public bool TryDeserialize(EventRecord eventRecord, [NotNullWhen(true)] out object? deserialized) {
#endif
		if (AutomaticDeserialization == AutomaticDeserialization.Disabled) {
			deserialized = null;
			return false;
		}

		var schemaDefinitionType = SchemaDefinitionTypeExtensions.FromContentType(eventRecord.ContentType);

		deserialized = SchemaRegistry.GetSerializer(schemaDefinitionType)
			.Deserialize(eventRecord.Data, eventRecord.EventType);

		return deserialized != null;
	}

	public static SerializationContext From(KurrentClientSerializationSettings? settings = null) {
		settings ??= new KurrentClientSerializationSettings();

		return new SerializationContext(
			SchemaRegistry.From(settings),
			settings.DefaultSerializationType,
			settings.AutomaticDeserialization
		);
	}
}
