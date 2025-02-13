namespace EventStore.Client.Serialization;

public interface ISerializer {
	public ReadOnlyMemory<byte> Serialize(object value);

	public object? Deserialize(ReadOnlyMemory<byte> data, Type type);
}
