namespace EventStore.Client.Serialization;

public interface ISchemaSerializer {
	public (ReadOnlyMemory<byte> Bytes, string typeName) Serialize(object value);

	public object? Deserialize(ReadOnlyMemory<byte> data, string typeName);
}
