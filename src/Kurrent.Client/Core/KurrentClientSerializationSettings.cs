using System.Text.Json;
using System.Text.Json.Serialization;
using EventStore.Client.Serialization;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client;

public enum AutomaticDeserialization {
	Disabled = 0,
	Enabled  = 1
}

public class KurrentClientSerializationSettings {
	public ISerializer?                    JsonSerializer           { get; set; }
	public ISerializer?                    BytesSerializer          { get; set; }
	public ContentType                     DefaultContentType       { get; set; } = ContentType.Json;
	public AutomaticDeserialization        AutomaticDeserialization { get; set; } = AutomaticDeserialization.Disabled;
	public IMessageTypeResolutionStrategy? MessageTypeResolutionStrategy { get; set; }

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
