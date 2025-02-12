using EventStore.Client.Serialization;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client;

public enum AutomaticDeserialization {
	Disabled = 0,
	Enabled  = 1
}

public class KurrentClientSerializationSettings {
	public ISerializer?                JsonSerializer            { get; set; }
	public ISerializer?                BytesSerializer           { get; set; }
	public ContentType                 DefaultContentType        { get; set; } = ContentType.Json;
	public IMessageTypeNamingStrategy? MessageTypeNamingStrategy { get; set; }
	public IDictionary<Type, string>   MessageTypeMap            { get; set; } = new Dictionary<Type, string>();
	public IDictionary<string, Type[]> CategoryMessageTypesMap   { get; set; } = new Dictionary<string, Type[]>();
	public Type?                       DefaultMetadataType       { get; set; }

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
		MessageTypeNamingStrategy = messageTypeNamingStrategy;

		return this;
	}

	public KurrentClientSerializationSettings RegisterMessageTypeForCategory<T>(string categoryName) =>
		RegisterMessageTypeForCategory(categoryName, typeof(T));

	public KurrentClientSerializationSettings RegisterMessageTypeForCategory(string categoryName, params Type[] types) {
		CategoryMessageTypesMap[categoryName] = CategoryMessageTypesMap.TryGetValue(categoryName, out var current)
			? [..current, ..types]
			: types;

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

	public KurrentClientSerializationSettings UseMetadataType<T>() =>
		UseMetadataType(typeof(T));

	public KurrentClientSerializationSettings UseMetadataType(Type type) {
		DefaultMetadataType = type;

		return this;
	}

	internal KurrentClientSerializationSettings Clone() {
		return new KurrentClientSerializationSettings {
			BytesSerializer           = BytesSerializer,
			JsonSerializer            = JsonSerializer,
			DefaultContentType        = DefaultContentType,
			MessageTypeMap            = new Dictionary<Type, string>(MessageTypeMap),
			CategoryMessageTypesMap   = new Dictionary<string, Type[]>(CategoryMessageTypesMap),
			MessageTypeNamingStrategy = MessageTypeNamingStrategy
		};
	}
}

public class OperationSerializationSettings {
	public AutomaticDeserialization AutomaticDeserialization { get; private set; } = AutomaticDeserialization.Enabled;
	public Action<KurrentClientSerializationSettings>? ConfigureSettings { get; private set; }

	public static readonly OperationSerializationSettings Disabled = new OperationSerializationSettings {
		AutomaticDeserialization = AutomaticDeserialization.Disabled
	};

	public static OperationSerializationSettings Configure(Action<KurrentClientSerializationSettings> configure) =>
		new OperationSerializationSettings {
			AutomaticDeserialization = AutomaticDeserialization.Enabled,
			ConfigureSettings        = configure
		};
}
