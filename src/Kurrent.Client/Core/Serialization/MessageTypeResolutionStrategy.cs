using System.Diagnostics.CodeAnalysis;
using Kurrent.Client.Tests.Streams.Serialization;
using Kurrent.Diagnostics.Tracing;

namespace EventStore.Client.Serialization;

public interface IMessageTypeNamingStrategy {
	string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext);

#if NET48
	bool TryResolveClrType(string messageTypeName, out Type? type);
#else
	bool TryResolveClrType(string messageTypeName, [NotNullWhen(true)] out Type? type);
#endif
	
	
#if NET48
	bool TryResolveClrMetadataType(string messageTypeName, out Type? type);
#else
	bool TryResolveClrMetadataType(string messageTypeName, [NotNullWhen(true)] out Type? type);
#endif
}

public record MessageTypeNamingResolutionContext(string CategoryName);

public class MessageTypeNamingStrategyWrapper(
	IMessageTypeRegistry messageTypeRegistry,
	IMessageTypeNamingStrategy messageTypeNamingStrategy
) : IMessageTypeNamingStrategy {
	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) {
		return messageTypeRegistry.GetOrAddTypeName(
			messageType,
			_ => messageTypeNamingStrategy.ResolveTypeName(messageType, resolutionContext)
		);
	}

#if NET48
	public bool TryResolveClrType(string messageTypeName, out Type? type) {
#else
	public bool TryResolveClrType(string messageTypeName, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeRegistry.GetOrAddClrType(
			messageTypeName,
			_ => messageTypeNamingStrategy.TryResolveClrType(messageTypeName, out var resolvedType)
				? resolvedType
				: null
		);

		return type != null;
	}

#if NET48
	public bool TryResolveClrMetadataType(string messageTypeName, out Type? type) {
#else
	public bool TryResolveClrMetadataType(string messageTypeName, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeRegistry.GetOrAddClrType(
			$"{messageTypeName}-metadata",
			_ => messageTypeNamingStrategy.TryResolveClrMetadataType(messageTypeName, out var resolvedType)
				? resolvedType
				: null
		);

		return type != null;
	}
}

public class DefaultMessageTypeNamingStrategy(Type? defaultMetadataType) : IMessageTypeNamingStrategy {
	readonly Type _defaultMetadataType = defaultMetadataType ?? typeof(TracingMetadata);
	
	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) =>
		$"{resolutionContext.CategoryName}-{messageType.FullName}"; 

#if NET48
	public bool TryResolveClrType(string messageTypeName, out Type? type) {
#else
	public bool TryResolveClrType(string messageTypeName, [NotNullWhen(true)] out Type? type) {
#endif
		var categorySeparatorIndex = messageTypeName.IndexOf('-');

		if (categorySeparatorIndex == -1 || categorySeparatorIndex == messageTypeName.Length - 1) {
			type = null;
			return false;
		}

		var clrTypeName = messageTypeName[(categorySeparatorIndex + 1)..];
		
		type = TypeProvider.GetTypeByFullName(clrTypeName);

		return type != null;
	}

#if NET48
	public bool TryResolveClrMetadataType(string messageTypeName, out Type? type) {
#else
	public bool TryResolveClrMetadataType(string messageTypeName, [NotNullWhen(true)] out Type? type) {
#endif
		type = _defaultMetadataType;
		return true;
	}
}
