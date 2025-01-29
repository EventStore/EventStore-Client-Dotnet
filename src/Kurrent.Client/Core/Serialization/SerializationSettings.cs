namespace Kurrent.Client.Core.Serialization;


public abstract class SerializationSettings {
	public abstract ContentType SchemaType { get; }
}

public class JsonSerializationSettings : SerializationSettings {
	public override ContentType SchemaType { get => ContentType.Json; }
}

public class BytesSerializationSettings : SerializationSettings {
	public override ContentType SchemaType { get => ContentType.Bytes; }
}
