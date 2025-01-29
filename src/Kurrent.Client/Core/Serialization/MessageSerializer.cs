using System.Diagnostics.CodeAnalysis;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Tests.Streams.Serialization;

namespace EventStore.Client.Serialization;

public interface IMessageSerializer {
	public EventData Serialize(object value);

	public bool TryDeserialize(EventRecord messageRecord, out object? deserialized);
}

public class MessageSerializer(
	ContentType contentType,
	ISerializer serializer,
	ISerializer jsonSerializer,
	IMessageTypeResolutionStrategy messageTypeResolutionStrategy
) : IMessageSerializer {
	public EventData Serialize(object value) {
		var eventType = messageTypeResolutionStrategy.ResolveTypeName(value);
		var bytes     = serializer.Serialize(value);
		var metadata  = jsonSerializer.Serialize(SerializationMetadata.From(value.GetType()));

		return new EventData(Uuid.NewUuid(), eventType, bytes, metadata, contentType.ToMessageContentType());
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
