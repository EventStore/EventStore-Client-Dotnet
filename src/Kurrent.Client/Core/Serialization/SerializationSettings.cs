namespace Kurrent.Client.Core.Serialization;


public abstract class SerializationSettings {
	public abstract SchemaDefinitionType SchemaType { get; }
}

public class JsonSerializationSettings : SerializationSettings {
	public override SchemaDefinitionType SchemaType { get => SchemaDefinitionType.Json; }
}

public class BytesSerializationSettings : SerializationSettings {
	public override SchemaDefinitionType SchemaType { get => SchemaDefinitionType.Bytes; }
}
