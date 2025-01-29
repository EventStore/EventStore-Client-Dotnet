using System.Diagnostics.CodeAnalysis;
using Kurrent.Client.Tests.Streams.Serialization;

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
		)!;
	}

#if NET48
	public bool TryResolveClrType(EventRecord messageRecord, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord messageRecord, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeMapper.GetOrAddClrType(
			messageRecord.EventType,
			TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly
		);

		return type != null;
	}
}
