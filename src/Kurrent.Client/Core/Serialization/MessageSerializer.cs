using EventStore.Client.Diagnostics;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client.Serialization;

public interface IMessageSerializer {
	public EventData Serialize(Message value, MessageSerializationContext context);

	public bool TryDeserialize(EventRecord messageRecord, out object? deserialized);
}

public class MessageSerializer(
	ContentType contentType,
	ISerializer serializer,
	ISerializer jsonSerializer,
	IMessageTypeResolutionStrategy messageTypeResolutionStrategy
) : IMessageSerializer {
	readonly string _messageContentType = contentType.ToMessageContentType();
	
	public EventData Serialize(Message message, MessageSerializationContext serializationContext) {
		var (data, metadata, eventId) = message;
		var eventType          = messageTypeResolutionStrategy.ResolveTypeName(message, serializationContext);
		var serializedData     = serializer.Serialize(data);
		var serializedMetadata = metadata != null ? jsonSerializer.Serialize(metadata) : ReadOnlyMemory<byte>.Empty;

		return new EventData(
			eventId,
			eventType,
			serializedData,
			serializedMetadata.InjectSerializationMetadata(SerializationMetadata.From(data.GetType())).ToArray(),
			_messageContentType
		);
	}

	public bool TryDeserialize(EventRecord messageRecord, out object? deserialized) {
		if (!messageTypeResolutionStrategy.TryResolveClrType(messageRecord, out var clrType)) {
			deserialized = null;
			return false;
		}

		deserialized = serializer.Deserialize(messageRecord.Data, clrType!);

		return true;
	}
}
