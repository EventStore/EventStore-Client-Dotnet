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
}

public class MessageSerializer(SchemaRegistry schemaRegistry) : IMessageSerializer {
	readonly ISerializer _jsonSerializer =
		schemaRegistry.GetSerializer(ContentType.Json);

	readonly IMessageTypeResolutionStrategy _messageTypeResolutionStrategy =
		schemaRegistry.MessageTypeResolutionStrategy;

	public EventData Serialize(Message message, MessageSerializationContext serializationContext) {
		var (data, metadata, eventId) = message;

		var eventType = _messageTypeResolutionStrategy
			.ResolveTypeName(message, serializationContext);

		var serializedData = schemaRegistry
			.GetSerializer(serializationContext.ContentType)
			.Serialize(data);

		var serializedMetadata = metadata != null
			? _jsonSerializer.Serialize(metadata)
			: ReadOnlyMemory<byte>.Empty;

		var metadataWithSerialization = serializedMetadata
			.InjectSerializationMetadata(SerializationMetadata.From(data.GetType()))
			.ToArray();

		return new EventData(
			eventId,
			eventType,
			serializedData,
			metadataWithSerialization,
			serializationContext.ContentType.ToMessageContentType()
		);
	}
	
#if NET48
	public bool TryDeserialize(EventRecord messageRecord, out object? deserialized) {
#else
	public bool TryDeserialize(EventRecord messageRecord, [NotNullWhen(true)] out object? deserialized) {
#endif
		if (!schemaRegistry
			    .MessageTypeResolutionStrategy
			    .TryResolveClrType(messageRecord, out var clrType)) {
			deserialized = null;
			return false;
		}

		deserialized = schemaRegistry
			.GetSerializer(FromMessageContentType(messageRecord.ContentType))
			.Deserialize(messageRecord.Data, clrType!);

		return deserialized != null;
	}
}
