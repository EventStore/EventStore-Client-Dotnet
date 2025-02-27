using System.Diagnostics.CodeAnalysis;
using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

using static ContentTypeExtensions;

interface IMessageSerializer {
	public EventData Serialize(Message value, MessageSerializationContext context);

#if NET48
	public bool TryDeserialize(EventRecord record, out Message? deserialized);
#else
	public bool TryDeserialize(EventRecord record, [NotNullWhen(true)] out Message? deserialized);
#endif
}

record MessageSerializationContext(
	string StreamName,
	ContentType ContentType
) {
	public string CategoryName =>
		StreamName.Split('-').FirstOrDefault() ?? "no_stream_category";
}

static class MessageSerializerExtensions {
	public static EventData[] Serialize(
		this IMessageSerializer serializer,
		IEnumerable<Message> messages,
		MessageSerializationContext context
	) {
		return messages.Select(m => serializer.Serialize(m, context)).ToArray();
	}

	public static IMessageSerializer With(
		this IMessageSerializer defaultMessageSerializer,
		KurrentClientSerializationSettings defaultSettings,
		OperationSerializationSettings? operationSettings
	) {
		if (operationSettings == null)
			return defaultMessageSerializer;

		if (operationSettings.AutomaticDeserialization == AutomaticDeserialization.Disabled)
			return NullMessageSerializer.Instance;

		if (operationSettings.ConfigureSettings == null)
			return defaultMessageSerializer;

		var settings = defaultSettings.Clone();
		operationSettings.ConfigureSettings.Invoke(settings);

		return new MessageSerializer(SchemaRegistry.From(settings));
	}
}

class MessageSerializer(SchemaRegistry schemaRegistry) : IMessageSerializer {
	readonly ISerializer _jsonSerializer =
		schemaRegistry.GetSerializer(ContentType.Json);

	readonly IMessageTypeNamingStrategy _messageTypeNamingStrategy =
		schemaRegistry.MessageTypeNamingStrategy;

	public EventData Serialize(Message message, MessageSerializationContext serializationContext) {
		var (data, metadata, eventId) = message;

		var eventType = _messageTypeNamingStrategy
			.ResolveTypeName(
				message.Data.GetType(),
				new MessageTypeNamingResolutionContext(serializationContext.CategoryName)
			);

		var serializedData = schemaRegistry
			.GetSerializer(serializationContext.ContentType)
			.Serialize(data);

		var serializedMetadata = metadata != null
			? _jsonSerializer.Serialize(metadata)
			: ReadOnlyMemory<byte>.Empty;

		return new EventData(
			eventId ?? Uuid.NewUuid(),
			eventType,
			serializedData,
			serializedMetadata,
			serializationContext.ContentType.ToMessageContentType()
		);
	}

#if NET48
	public bool TryDeserialize(EventRecord record, out Message? deserialized) {
#else
	public bool TryDeserialize(EventRecord record, [NotNullWhen(true)] out Message? deserialized) {
#endif
		if (!TryResolveClrType(record, out var clrType)) {
			deserialized = null;
			return false;
		}

		var data = schemaRegistry
			.GetSerializer(FromMessageContentType(record.ContentType))
			.Deserialize(record.Data, clrType!);

		if (data == null) {
			deserialized = null;
			return false;
		}

		object? metadata = record.Metadata.Length > 0 && TryResolveClrMetadataType(record, out var clrMetadataType)
				? _jsonSerializer.Deserialize(record.Metadata, clrMetadataType!)
				: null;

		deserialized = Message.From(data, metadata, record.EventId);
		return true;
	}

	public static MessageSerializer From(KurrentClientSerializationSettings? settings = null) {
		settings ??= KurrentClientSerializationSettings.Default();

		return new MessageSerializer(SchemaRegistry.From(settings));
	}

	bool TryResolveClrType(EventRecord record, out Type? clrType) =>
		schemaRegistry
			.MessageTypeNamingStrategy
			.TryResolveClrType(record.EventType, out clrType);

	bool TryResolveClrMetadataType(EventRecord record, out Type? clrMetadataType) =>
		schemaRegistry
			.MessageTypeNamingStrategy
			.TryResolveClrMetadataType(record.EventType, out clrMetadataType);
}

class NullMessageSerializer : IMessageSerializer {
	public static readonly NullMessageSerializer Instance = new NullMessageSerializer();

	public EventData Serialize(Message value, MessageSerializationContext context) {
		throw new InvalidOperationException("Cannot serialize, automatic deserialization is disabled");
	}

#if NET48
	public bool TryDeserialize(EventRecord record, out Message? deserialized) {
#else
	public bool TryDeserialize(EventRecord eventRecord, [NotNullWhen(true)] out Message? deserialized) {
#endif
		deserialized = null;
		return false;
	}
}
