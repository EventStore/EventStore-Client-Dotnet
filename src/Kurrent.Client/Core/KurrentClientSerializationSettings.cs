using System.Text.Json;
using System.Text.Json.Serialization;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client;

public enum AutomaticDeserialization {
	Disabled = 0,
	Enabled  = 1
}

public class KurrentClientSerializationSettings {
	public JsonSerializationSettings  Json  { get; set; } = new SystemTextJsonSerializationSettings();
	public BytesSerializationSettings Bytes { get; set; } = new BytesSerializationSettings();

	public AutomaticDeserialization AutomaticDeserialization { get; set; } = AutomaticDeserialization.Disabled;

	public static KurrentClientSerializationSettings Default(
		Action<KurrentClientSerializationSettings>? configure = null
	) {
		var settings = new KurrentClientSerializationSettings();

		configure?.Invoke(settings);

		return settings;
	}

	public KurrentClientSerializationSettings WithJsonSettings(JsonSerializationSettings jsonSerializationSettings) {
		Json = jsonSerializationSettings;

		return this;
	}

	public KurrentClientSerializationSettings WithBytesSettings(BytesSerializationSettings bytesSerialization) {
		Bytes = bytesSerialization;

		return this;
	}

	public KurrentClientSerializationSettings EnableAutomaticDeserialization() {
		AutomaticDeserialization = AutomaticDeserialization.Enabled;

		return this;
	}
}
