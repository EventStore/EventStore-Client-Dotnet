using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Tests.Streams.Serialization;

namespace EventStore.Client.Serialization;

public interface IMessageSerializer {
	public EventData Serialize(object value);

	public bool TryDeserialize(EventRecord messageRecord, out object? deserialized);
}

public interface IMessageTypeResolutionStrategy {
	string ResolveTypeName(object messageData);

#if NET48
	bool TryResolveClrType(EventRecord messageRecord, out Type? type);
#else
	bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type);
#endif
}

public class DefaultMessageTypeResolutionStrategy : IMessageTypeResolutionStrategy {
	public string ResolveTypeName(object messageData) {
		return EventTypeMapper.Instance.ToName(messageData.GetType());
	}

#if NET48
	public bool TryResolveClrType(EventRecord messageRecord, out Type? type){
#else
	public bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type) {
#endif
		type = EventTypeMapper.Instance.ToType(messageRecord.EventType);

		return type != null;
	}
}

public record MessageDefaultMetadata(
	[property: JsonPropertyName("$clrTypeAssemblyQualifiedName")]
	string? MessageTypeAssemblyQualifiedName,
	[property: JsonPropertyName("$clrTypeName")]
	string? MessageTypeClrTypeName
) {
	public static MessageDefaultMetadata From(Type clrType) =>
		new MessageDefaultMetadata(clrType.AssemblyQualifiedName, clrType.Name);
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
		var metadata  = jsonSerializer.Serialize(MessageDefaultMetadata.From(value.GetType()));

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
