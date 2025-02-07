using System.Diagnostics.CodeAnalysis;
using EventStore.Client.Diagnostics;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client.Serialization;

using static ContentTypeExtensions;

public interface IMessageSerializer {
	public EventData Serialize(Message value, MessageSerializationContext context);

#if NET48
	public bool TryDeserialize(EventRecord messageRecord, out object? deserialized);
#else
	public bool TryDeserialize(EventRecord messageRecord, [NotNullWhen(true)] out object? deserialized);
#endif
}

public record MessageSerializationContext(
	string StreamName,
	ContentType ContentType
) {
	public string CategoryName =>
		// TODO: This is dangerous, as separator can be changed in database settings
		StreamName.Split('-').FirstOrDefault() ?? "no_stream_category";
}

public static class MessageSerializerExtensions {
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
		OperationSerializationSettings? operationSettings = null
	) {
		if (operationSettings == null)
			return defaultMessageSerializer;
		
		if (operationSettings.AutomaticDeserialization == AutomaticDeserialization.Disabled)
			return NulloMessageSerializer.Instance;

		if (operationSettings.ConfigureSettings == null)
			return defaultMessageSerializer;

		var settings = defaultSettings.Clone();
		operationSettings.ConfigureSettings.Invoke(settings);

		return new MessageSerializer(SchemaRegistry.From(settings));
	}
}

public class MessageSerializer(SchemaRegistry schemaRegistry) : IMessageSerializer {
	readonly ISerializer _jsonSerializer =
		schemaRegistry.GetSerializer(ContentType.Json);

	readonly IMessageTypeNamingStrategy _messageTypeNamingStrategy =
		schemaRegistry.MessageTypeNamingStrategy;

	public EventData Serialize(Message message, MessageSerializationContext serializationContext) {
		var (data, metadata, eventId) = message;

		var eventType = _messageTypeNamingStrategy
			.ResolveTypeName(message.Data.GetType(), serializationContext);

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
	public bool TryDeserialize(EventRecord messageRecord, out object? deserialized) {
#else
	public bool TryDeserialize(EventRecord messageRecord, [NotNullWhen(true)] out object? deserialized) {
#endif
		if (!schemaRegistry
			    .MessageTypeNamingStrategy
			    .TryResolveClrType(messageRecord.EventType, out var clrType)) {
			deserialized = null;
			return false;
		}

		deserialized = schemaRegistry
			.GetSerializer(FromMessageContentType(messageRecord.ContentType))
			.Deserialize(messageRecord.Data, clrType!);

		return deserialized != null;
	}

	public static MessageSerializer From(KurrentClientSerializationSettings? settings = null) {
		settings ??= KurrentClientSerializationSettings.Default();

		return new MessageSerializer(SchemaRegistry.From(settings));
	}
}

public class NulloMessageSerializer : IMessageSerializer {
	public static readonly NulloMessageSerializer Instance = new NulloMessageSerializer();
	
	public EventData Serialize(Message value, MessageSerializationContext context) {
		throw new InvalidOperationException("Cannot serialize, automatic deserialization is disabled");
	}

#if NET48
	public bool TryDeserialize(EventRecord eventRecord, out object? deserialized) {
#else
	public bool TryDeserialize(EventRecord eventRecord, [NotNullWhen(true)] out object? deserialized) {
#endif
		deserialized = null;
		return false;
	}
}
