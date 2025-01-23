using Kurrent.Client.Tests.Streams.Serialization;

namespace EventStore.Client.Serialization;

public class SchemaSerializer(ISerializer serializer, IEventTypeMapper eventTypeMapper) : ISchemaSerializer {
	public (ReadOnlyMemory<byte> Bytes, string typeName) Serialize(object value) {
		var eventType = eventTypeMapper.ToName(value.GetType());
		var bytes     = serializer.Serialize(value);

		return (bytes, eventType);
	}

	public object? Deserialize(ReadOnlyMemory<byte> data, string typeName) {
		var clrType = eventTypeMapper.ToType(typeName);
		return clrType != null ? serializer.Deserialize(data, clrType) : null;
	}
}
