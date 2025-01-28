using EventStore.Client;
using EventStore.Client.Serialization;
using Kurrent.Client.Tests.Streams.Serialization;

namespace Kurrent.Client.Core.Serialization;

using static Constants.Metadata.ContentTypes;

public enum SchemaDefinitionType {
	Undefined = 0,
	Json      = 1,

	// Protobuf  = 2,
	// Avro      = 3,
	Bytes = 4
}

public static class SchemaDefinitionTypeExtensions {
	public static SchemaDefinitionType FromContentType(string contentType) =>
		contentType == ApplicationJson
			? SchemaDefinitionType.Json
			: SchemaDefinitionType.Bytes;
}

// TODO: We need to discuss how to include the full Schema Registry code here
public class SchemaRegistry(
	IDictionary<SchemaDefinitionType, SchemaSerializer> serializers
) {
	public ISchemaSerializer GetSerializer(SchemaDefinitionType schemaType) =>
		serializers[schemaType];

	public static SchemaRegistry From(KurrentClientSerializationSettings settings) {
		var typeMapper = EventTypeMapper.Instance;

		var serializers = new Dictionary<SchemaDefinitionType, SchemaSerializer> {
			{
				SchemaDefinitionType.Json,
				new SchemaSerializer(settings.JsonSerializer ?? new SystemTextJsonSerializer(), typeMapper)
			}, {
				SchemaDefinitionType.Bytes,
				new SchemaSerializer(settings.BytesSerializer ?? new SystemTextJsonSerializer(), typeMapper)
			}
		};

		return new SchemaRegistry(serializers);
	}
}
