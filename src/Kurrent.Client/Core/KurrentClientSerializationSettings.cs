using System.Text.Json;
using System.Text.Json.Serialization;
using EventStore.Client.Serialization;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client;

public enum AutomaticDeserialization {
	Disabled = 0,
	Enabled  = 1
}

public enum SerializationType {
	Json      = 1,
	// Protobuf  = 2,
	// Avro      = 3,
	Bytes = 4
}

public class KurrentClientSerializationSettings {
	public ISerializer?             JsonSerializer           { get; set; }
	public ISerializer?             BytesSerializer          { get; set; }
	public SerializationType        DefaultSerializationType { get; set; } = SerializationType.Json;
	public AutomaticDeserialization AutomaticDeserialization { get; set; } = AutomaticDeserialization.Disabled;

	public static KurrentClientSerializationSettings Default(
		Action<KurrentClientSerializationSettings>? configure = null
	) {
		var settings = new KurrentClientSerializationSettings();

		configure?.Invoke(settings);

		return settings;
	}

	public KurrentClientSerializationSettings UseJsonSettings(SystemTextJsonSerializationSettings jsonSerializationSettings) {
		JsonSerializer = new SystemTextJsonSerializer(jsonSerializationSettings);

		return this;
	}

	public KurrentClientSerializationSettings UseJsonSerializer(ISerializer serializer) {
		JsonSerializer = serializer;

		return this;
	}

	public KurrentClientSerializationSettings UseBytesSerializer(ISerializer serializer) {
		BytesSerializer = serializer;

		return this;
	}

	public KurrentClientSerializationSettings EnableAutomaticDeserialization() {
		AutomaticDeserialization = AutomaticDeserialization.Enabled;

		return this;
	}
}
