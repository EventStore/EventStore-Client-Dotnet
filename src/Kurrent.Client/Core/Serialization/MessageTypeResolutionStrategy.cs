using System.Diagnostics.CodeAnalysis;
using Kurrent.Client.Tests.Streams.Serialization;
using EventStore.Client.Diagnostics;

namespace EventStore.Client.Serialization;

public interface IMessageTypeResolutionStrategy {
	string ResolveTypeName(object messageData);

#if NET48
	bool TryResolveClrType(EventRecord messageRecord, out Type? type);
#else
	bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type);
#endif
}

public class MessageTypeResolutionStrategyWrapper(
	IMessageTypeMapper messageTypeMapper,
	IMessageTypeResolutionStrategy messageTypeResolutionStrategy
) : IMessageTypeResolutionStrategy {
	public string ResolveTypeName(object messageData) {
		return messageTypeMapper.GetOrAddTypeName(
			messageData.GetType(),
			_ => messageTypeResolutionStrategy.ResolveTypeName(messageData)
		);
	}

#if NET48
	public bool TryResolveClrType(EventRecord messageRecord, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeMapper.GetOrAddClrType(
			messageRecord.EventType,
			_ => messageTypeResolutionStrategy.TryResolveClrType(messageRecord, out var resolvedType)
				? resolvedType
				: null
		);

		return type != null;
	}
}

public class DefaultMessageTypeResolutionStrategy
	: IMessageTypeResolutionStrategy {
	public string ResolveTypeName(object messageData) =>
		messageData.GetType().FullName!;

#if NET48
	public bool TryResolveClrType(EventRecord messageRecord, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type) {
#endif
		var serializationMetadata = messageRecord.Metadata.ExtractSerializationMetadata();

		if (!serializationMetadata.IsValid) {
			type = null;
			return false;
		}

		type = Type.GetType(serializationMetadata.MessageTypeAssemblyQualifiedName!)
		    ?? TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly(
			       serializationMetadata.MessageTypeClrTypeName!
		       );

		return type != null;
	}
}
