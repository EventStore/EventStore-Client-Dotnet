using System.Diagnostics.CodeAnalysis;
using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

public class DeserializationContext(
	SchemaRegistry schemaRegistry,
	AutomaticDeserialization automaticDeserialization
) {
#if NET48
	public bool TryDeserialize(EventRecord eventRecord, out object? deserialized) {
#else
	public bool TryDeserialize(EventRecord eventRecord, [NotNullWhen(true)] out object? deserialized) {
#endif
		if (automaticDeserialization == AutomaticDeserialization.Disabled) {
			deserialized = null;
			return false;
		}

		var schemaDefinitionType = SchemaDefinitionTypeExtensions.FromContentType(eventRecord.ContentType);

		deserialized = schemaRegistry.GetSerializer(schemaDefinitionType)
			.Deserialize(eventRecord.Data, eventRecord.EventType);

		return deserialized != null;
	}
}
