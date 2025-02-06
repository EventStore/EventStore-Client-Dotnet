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
	public ISerializer?                    JsonSerializer { get; set; }
	public ISerializer?                    BytesSerializer { get; set; }
	public ContentType                     DefaultContentType { get; set; } = ContentType.Json;
	public AutomaticDeserialization        AutomaticDeserialization { get; set; } = AutomaticDeserialization.Disabled;
	public IMessageTypeNamingStrategy? MessageTypeResolutionStrategy { get; set; }

	public IDictionary<Type, string> MessageTypeMap { get; set; } = new Dictionary<Type, string>();

	public static KurrentClientSerializationSettings Default(
		Action<KurrentClientSerializationSettings>? configure = null
	) {
		var settings = new KurrentClientSerializationSettings();

		configure?.Invoke(settings);

		return settings;
	}

	public KurrentClientSerializationSettings UseJsonSettings(
		SystemTextJsonSerializationSettings jsonSerializationSettings
	) {
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

	public KurrentClientSerializationSettings UseMessageTypeResolutionStrategy<TCustomMessageTypeResolutionStrategy>()
		where TCustomMessageTypeResolutionStrategy : IMessageTypeNamingStrategy, new() =>
		UseMessageTypeResolutionStrategy(new TCustomMessageTypeResolutionStrategy());

	public KurrentClientSerializationSettings UseMessageTypeResolutionStrategy(
		IMessageTypeNamingStrategy messageTypeNamingStrategy
	) {
		MessageTypeResolutionStrategy = messageTypeNamingStrategy;

		return this;
	}

	public KurrentClientSerializationSettings RegisterMessageType<T>(string typeName) =>
		RegisterMessageType(typeof(T), typeName);

	public KurrentClientSerializationSettings RegisterMessageType(Type type, string typeName) {
		MessageTypeMap.Add(type, typeName);

		return this;
	}

	public KurrentClientSerializationSettings RegisterMessageTypes(IDictionary<Type, string> typeMap) {
		foreach (var map in typeMap) {
			MessageTypeMap.Add(map.Key, map.Value);
		}

		return this;
	}

	public KurrentClientSerializationSettings EnableAutomaticDeserialization() {
		AutomaticDeserialization = AutomaticDeserialization.Enabled;

		return this;
	}
}
