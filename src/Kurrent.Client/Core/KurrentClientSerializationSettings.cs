namespace EventStore.Client;

public enum SchemaDefinitionType {
	Undefined = 0,
	Json      = 1,
	Protobuf  = 2,
	Avro      = 3,
	Bytes     = 4
}

public abstract class KurrentClientSerializationSettings {
	public abstract SchemaDefinitionType SchemaType { get; }

	public static KurrentClientSerializationSettings Default(
		SchemaDefinitionType schemaDefinitionType = SchemaDefinitionType.Json
	) =>
		schemaDefinitionType switch {
			SchemaDefinitionType.Json      => new SystemTextJsonSerializationSettings(),
			SchemaDefinitionType.Protobuf  => throw new NotImplementedException("Not implemented yet, sorry!"),
			SchemaDefinitionType.Avro      => throw new NotImplementedException("Not implemented yet, sorry!"),
			SchemaDefinitionType.Bytes     => new BytesSerializationSettings(),
			SchemaDefinitionType.Undefined => throw new NotImplementedException("Not implemented yet, sorry!"),
			_                              => throw new ArgumentOutOfRangeException(nameof(schemaDefinitionType), schemaDefinitionType, null)
		};
	
	public static JSONSerializationSettings Json() => 
		new JSONSerializationSettings();
	
	public static BytesSerializationSettings Bytes() => 
		new BytesSerializationSettings();
	
	public static CustomSerializationSettings Custom() => 
		new CustomSerializationSettings();
}

public class JSONSerializationSettings: KurrentClientSerializationSettings {
	public override SchemaDefinitionType SchemaType { get => SchemaDefinitionType.Json; }
}

public class SystemTextJsonSerializationSettings: JSONSerializationSettings {
}

public class BytesSerializationSettings: KurrentClientSerializationSettings {
	public override SchemaDefinitionType SchemaType { get => SchemaDefinitionType.Bytes; }
}

public class CustomSerializationSettings: KurrentClientSerializationSettings {
	public override SchemaDefinitionType SchemaType { get => SchemaDefinitionType.Undefined; }
}
