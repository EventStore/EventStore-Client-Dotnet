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

public class DefaultMessageTypeResolutionStrategy(IMessageTypeMapper messageTypeMapper)
	: IMessageTypeResolutionStrategy {
	public string ResolveTypeName(object messageData) {
		return messageTypeMapper.GetOrAddTypeName(
			messageData.GetType(),
			clrType => clrType.FullName!
		);
	}

#if NET48
	public bool TryResolveClrType(EventRecord messageRecord, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeMapper.GetOrAddClrType(
			messageRecord.EventType,
			_ => {
				var serializationMetadata = messageRecord.Metadata.ExtractSerializationMetadata();

				if (!serializationMetadata.IsValid)
					return null;

				return Type.GetType(
					serializationMetadata.MessageTypeAssemblyQualifiedName
				 ?? serializationMetadata.MessageTypeClrTypeName!
				);
			}
		);

		return type != null;
	}
}
