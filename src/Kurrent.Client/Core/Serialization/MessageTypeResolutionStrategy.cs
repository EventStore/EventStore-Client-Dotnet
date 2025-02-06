using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Kurrent.Client.Tests.Streams.Serialization;
using EventStore.Client.Diagnostics;
using Kurrent.Client.Core.Serialization;

namespace EventStore.Client.Serialization;

public interface IMessageTypeNamingStrategy {
	string ResolveTypeName(Message message, MessageSerializationContext serializationContext);

#if NET48
	bool TryResolveClrType(EventRecord messageRecord, out Type? type);
#else
	bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type);
#endif
}

public class MessageTypeNamingStrategyWrapper(
	IMessageTypeRegistry messageTypeRegistry,
	IMessageTypeNamingStrategy messageTypeNamingStrategy
) : IMessageTypeNamingStrategy {
	public string ResolveTypeName(Message message, MessageSerializationContext serializationContext) {
		return messageTypeRegistry.GetOrAddTypeName(
			message.Data.GetType(),
			_ => messageTypeNamingStrategy.ResolveTypeName(message, serializationContext)
		);
	}

#if NET48
	public bool TryResolveClrType(EventRecord messageRecord, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeRegistry.GetOrAddClrType(
			messageRecord.EventType,
			_ => messageTypeNamingStrategy.TryResolveClrType(messageRecord, out var resolvedType)
				? resolvedType
				: null
		);

		return type != null;
	}
}

public class DefaultMessageTypeNamingStrategy
	: IMessageTypeNamingStrategy {
	public string ResolveTypeName(Message message, MessageSerializationContext serializationContext) =>
		$"{serializationContext.CategoryName}-{JsonNamingPolicy.SnakeCaseLower.ConvertName(message.Data.GetType().Name.ToLower())}"; 

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
