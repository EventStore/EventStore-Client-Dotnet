using System.Text.Json;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client;

/// <summary>
/// Provides configuration options for messages serialization and deserialization in the KurrentDB client.
/// </summary>
public class KurrentClientSerializationSettings {
	/// <summary>
	/// The serializer responsible for handling JSON-formatted data. This serializer is used both for
	/// serializing outgoing JSON messages and deserializing incoming JSON messages. If not specified,
	/// a default System.Text.Json serializer will be used with standard settings.
	/// <br/>
	/// That also allows you to bring your custom JSON serializer implementation (e.g. JSON.NET)
	/// </summary>
	public ISerializer? JsonSerializer { get; set; }

	/// <summary>
	/// The serializer responsible for handling binary data formats. This is used when working with
	/// binary-encoded messages rather than text-based formats (e.g. Protobuf or Avro). Required when storing
	/// or retrieving content with "application/octet-stream"  content type
	/// </summary>
	public ISerializer? BytesSerializer { get; set; }

	/// <summary>
	/// Determines which serialization format (JSON or binary) is used by default when writing messages
	/// where the content type isn't explicitly specified. The default content type is "application/json"
	/// </summary>
	public ContentType DefaultContentType { get; set; } = ContentType.Json;

	/// <summary>
	/// Defines the custom strategy used to map between the type name stored in messages and .NET type names.
	/// If not provided the default <see cref="Kurrent.Client.Core.Serialization.DefaultMessageTypeNamingStrategy"/> will be used.
	/// It resolves the CLR type name to the format: "{stream category name}-{CLR Message Type}".
	/// You can provide your own implementation of <see cref="Kurrent.Client.Core.Serialization.IMessageTypeNamingStrategy"/>
	/// and register it here to override the default behaviour
	/// </summary>
	public IMessageTypeNamingStrategy? MessageTypeNamingStrategy { get; set; }

	/// <summary>
	/// Allows to register mapping of CLR message types to their corresponding message type names used in serialized messages.
	/// </summary>
	public IDictionary<Type, string> MessageTypeMap { get; set; } = new Dictionary<Type, string>();

	/// <summary>
	/// Registers CLR message types that can be appended to the specific stream category.
	/// Types will have message type names resolved based on the used <see cref="Kurrent.Client.Core.Serialization.IMessageTypeNamingStrategy"/>
	/// </summary>
	public IDictionary<string, Type[]> CategoryMessageTypesMap { get; set; } = new Dictionary<string, Type[]>();

	/// <summary>
	/// Specifies the CLR type that should be used when deserializing metadata for all events.
	/// When set, the client will attempt to deserialize event metadata into this type.
	/// If not provided, <see cref="Kurrent.Diagnostics.Tracing.TracingMetadata"/> will be used.
	/// </summary>
	public Type? DefaultMetadataType { get; set; }

	/// <summary>
	/// Creates a new instance of serialization settings with either default values or custom configuration.
	/// This factory method is the recommended way to create serialization settings for the KurrentDB client.
	/// </summary>
	/// <param name="configure">Optional callback to customize the settings. If null, default settings are used.</param>
	/// <returns>A fully configured instance ready to be used with the KurrentDB client.</returns>
	/// <example>
	/// <code>
	/// var settings = KurrentClientSerializationSettings.Default(options => {
	///     options.RegisterMessageType&lt;UserCreated&gt;("user-created");
	///     options.RegisterMessageType&lt;UserUpdated&gt;("user-updated");
	///     options.RegisterMessageTypeForCategory&lt;UserCreated&gt;("user");
	/// });
	/// </code>
	/// </example>
	public static KurrentClientSerializationSettings Default(
		Action<KurrentClientSerializationSettings>? configure = null
	) {
		var settings = new KurrentClientSerializationSettings();

		configure?.Invoke(settings);

		return settings;
	}

	/// <summary>
	/// Configures the JSON serializer using custom options while inheriting from the default System.Text.Json settings.
	/// This allows fine-tuning serialization behavior such as case sensitivity, property naming, etc.
	/// </summary>
	/// <param name="configure">A function that receives the default options and returns modified options.</param>
	/// <returns>The current instance for method chaining.</returns>
	/// <example>
	/// <code>
	/// settings.UseJsonSettings(options => {
	///     options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	///     options.WriteIndented = true;
	///     options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
	///     return options;
	/// });
	/// </code>
	/// </example>
	public KurrentClientSerializationSettings UseJsonSettings(
		Func<JsonSerializerOptions, JsonSerializerOptions> configure
	) {
		JsonSerializer = new SystemTextJsonSerializer(
			new SystemTextJsonSerializationSettings
				{ Options = configure(SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions) }
		);

		return this;
	}

	/// <summary>
	/// Configures JSON serialization using provided System.Text.Json serializer options.
	/// </summary>
	/// <param name="systemTextJsonSerializerOptions">The JSON serializer options to use.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseJsonSettings(JsonSerializerOptions systemTextJsonSerializerOptions) {
		JsonSerializer = new SystemTextJsonSerializer(
			new SystemTextJsonSerializationSettings { Options = systemTextJsonSerializerOptions }
		);

		return this;
	}

	/// <summary>
	/// Configures JSON serialization using provided <see cref="Kurrent.Client.Core.Serialization.SystemTextJsonSerializationSettings"/>
	/// </summary>
	/// <param name="jsonSerializationSettings">The SystemTextJson serialization settings to use.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseJsonSettings(
		SystemTextJsonSerializationSettings jsonSerializationSettings
	) {
		JsonSerializer = new SystemTextJsonSerializer(jsonSerializationSettings);

		return this;
	}

	/// <summary>
	/// Sets a custom JSON serializer implementation. 
	/// That also allows you to bring your custom JSON serializer implementation (e.g. JSON.NET)
	/// </summary>
	/// <param name="serializer">The serializer to use for JSON content.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseJsonSerializer(ISerializer serializer) {
		JsonSerializer = serializer;

		return this;
	}

	/// <summary>
	/// Sets a custom binary serializer implementation.
	/// That also allows you to bring your custom binary serializer implementation (e.g. Protobuf or Avro)
	/// </summary>
	/// <param name="serializer">The serializer to use for binary content.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseBytesSerializer(ISerializer serializer) {
		BytesSerializer = serializer;

		return this;
	}

	/// <summary>
	/// Configures a custom message type naming strategy.
	/// </summary>
	/// <typeparam name="TCustomMessageTypeResolutionStrategy">The type of naming strategy to use.</typeparam>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseMessageTypeNamingStrategy<TCustomMessageTypeResolutionStrategy>()
		where TCustomMessageTypeResolutionStrategy : IMessageTypeNamingStrategy, new() =>
		UseMessageTypeNamingStrategy(new TCustomMessageTypeResolutionStrategy());

	/// <summary>
	/// Configures a custom message type naming strategy.
	/// </summary>
	/// <param name="messageTypeNamingStrategy">The naming strategy instance to use.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseMessageTypeNamingStrategy(
		IMessageTypeNamingStrategy messageTypeNamingStrategy
	) {
		MessageTypeNamingStrategy = messageTypeNamingStrategy;

		return this;
	}

	/// <summary>
	/// Associates a message type with a specific stream category to enable automatic deserialization.
	/// In event sourcing, streams are often prefixed with a category (e.g., "user-123", "order-456").
	/// This method tells the client which message types can appear in streams of a given category.
	/// </summary>
	/// <typeparam name="T">The event or message type that can appear in the category's streams.</typeparam>
	/// <param name="categoryName">The category prefix (e.g., "user", "order", "account").</param>
	/// <returns>The current instance for method chaining.</returns>
	/// <example>
	/// <code>
	/// // Register event types that can appear in user streams
	/// settings.RegisterMessageTypeForCategory&lt;UserCreated&gt;("user")
	///        .RegisterMessageTypeForCategory&lt;UserUpdated&gt;("user")
	///        .RegisterMessageTypeForCategory&lt;UserDeleted&gt;("user");
	/// </code>
	/// </example>
	public KurrentClientSerializationSettings RegisterMessageTypeForCategory<T>(string categoryName) =>
		RegisterMessageTypeForCategory(categoryName, typeof(T));

	/// <summary>
	/// Registers multiple message types for a specific stream category.
	/// </summary>
	/// <param name="categoryName">The category name to register the types with.</param>
	/// <param name="types">The message types to register.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings RegisterMessageTypeForCategory(string categoryName, params Type[] types) {
		CategoryMessageTypesMap[categoryName] = CategoryMessageTypesMap.TryGetValue(categoryName, out var current)
			? [..current, ..types]
			: types;

		return this;
	}

	/// <summary>
	/// Maps a .NET type to a specific message type name that will be stored in the message metadata.
	/// This mapping is used during automatic deserialization, as it tells the client which CLR type
	/// to instantiate when encountering a message with a particular type name in the database.
	/// </summary>
	/// <typeparam name="T">The .NET type to register (typically a message class).</typeparam>
	/// <param name="typeName">The string identifier to use for this type in the database.</param>
	/// <returns>The current instance for method chaining.</returns>
	/// <remarks>
	/// The type name is often different from the .NET type name to support versioning and evolution
	/// of your domain model without breaking existing stored messages.
	/// </remarks>
	/// <example>
	/// <code>
	/// // Register me types with their corresponding type identifiers
	/// settings.RegisterMessageType&lt;UserCreated&gt;("user-created-v1")
	///        .RegisterMessageType&lt;OrderPlaced&gt;("order-placed-v2");
	/// </code>
	/// </example>
	public KurrentClientSerializationSettings RegisterMessageType<T>(string typeName) =>
		RegisterMessageType(typeof(T), typeName);

	/// <summary>
	/// Registers a message type with a specific type name.
	/// </summary>
	/// <param name="type">The message type to register.</param>
	/// <param name="typeName">The type name to register for the message type.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings RegisterMessageType(Type type, string typeName) {
		MessageTypeMap[type] = typeName;

		return this;
	}

	/// <summary>
	/// Registers multiple message types with their corresponding type names.
	/// </summary>
	/// <param name="typeMap">Dictionary mapping types to their type names.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings RegisterMessageTypes(IDictionary<Type, string> typeMap) {
		foreach (var map in typeMap) {
			MessageTypeMap[map.Key] = map.Value;
		}

		return this;
	}

	/// <summary>
	/// Configures a strongly-typed metadata class for all mes in the system.
	/// This enables accessing metadata properties in a type-safe manner rather than using dynamic objects.
	/// </summary>
	/// <typeparam name="T">The metadata class type containing properties matching the expected metadata fields.</typeparam>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseMetadataType<T>() =>
		UseMetadataType(typeof(T));

	/// <summary>
	/// Configures a strongly-typed metadata class for all mes in the system.
	/// This enables accessing metadata properties in a type-safe manner rather than using dynamic objects.
	/// </summary>
	/// <param name="type">The metadata class type containing properties matching the expected metadata fields.</param>
	/// <returns>The current instance for method chaining.</returns>
	public KurrentClientSerializationSettings UseMetadataType(Type type) {
		DefaultMetadataType = type;

		return this;
	}

	/// <summary>
	/// Creates a deep copy of the current serialization settings.
	/// </summary>
	/// <returns>A new instance with copied settings.</returns>
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

/// <summary>
/// Provides operation-specific serialization settings that override the global client configuration
/// for individual operations like reading from or appending to streams. This allows fine-tuning
/// serialization behavior on a per-operation basis without changing the client-wide settings.
/// </summary>
public class OperationSerializationSettings {
	/// <summary>
	/// Controls whether mes should be automatically deserialized for this specific operation.
	/// When enabled (the default), messages will be converted to their appropriate CLR types.
	/// When disabled, messages will be returned in their raw serialized form.
	/// </summary>
	public AutomaticDeserialization AutomaticDeserialization { get; private set; } = AutomaticDeserialization.Enabled;

	/// <summary>
	/// A callback that allows customizing serialization settings for this specific operation.
	/// This can be used to override type mappings, serializers, or other settings just for
	/// the scope of a single operation without affecting other operations.
	/// </summary>
	public Action<KurrentClientSerializationSettings>? ConfigureSettings { get; private set; }

	/// <summary>
	/// A pre-configured settings instance that disables automatic deserialization.
	/// Use this when you need to access raw message data in its serialized form.
	/// </summary>
	public static readonly OperationSerializationSettings Disabled = new OperationSerializationSettings {
		AutomaticDeserialization = AutomaticDeserialization.Disabled
	};

	/// <summary>
	/// Creates operation-specific serialization settings with custom configuration while keeping
	/// automatic deserialization enabled. This allows operation-specific type mappings or
	/// serializer settings without changing the global client configuration.
	/// </summary>
	/// <param name="configure">A callback to customize serialization settings for this operation.</param>
	/// <returns>A configured instance of <see cref="OperationSerializationSettings"/> with enabled deserialization.</returns>
	public static OperationSerializationSettings Configure(Action<KurrentClientSerializationSettings> configure) =>
		new OperationSerializationSettings {
			AutomaticDeserialization = AutomaticDeserialization.Enabled,
			ConfigureSettings        = configure
		};
}

/// <summary>
/// Controls whether the KurrentDB client should automatically deserialize message payloads
/// into their corresponding CLR types based on the configured type mappings.
/// </summary>
public enum AutomaticDeserialization {
	/// <summary>
	/// Disables automatic deserialization. Messages will be returned in their raw serialized form,
	/// requiring manual deserialization by the application. Use this when you need direct access to the raw data
	/// or when working with messages that don't have registered type mappings.
	/// </summary>
	Disabled = 0,

	/// <summary>
	/// Enables automatic deserialization. The client will attempt to convert messages into their appropriate
	/// CLR types using the configured serializers and type mappings. This simplifies working with strongly-typed
	/// domain messages but requires proper type registration.
	/// </summary>
	Enabled = 1
}
