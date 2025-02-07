using System.Collections.Concurrent;

namespace Kurrent.Client.Tests.Streams.Serialization;

public interface IMessageTypeRegistry {
	void    Map(Type messageType, string messageTypeName);
	string? GetTypeName(Type messageType);
	string  GetOrAddTypeName(Type clrType, Func<Type, string> getTypeName);
	Type?   GetClrType(string messageTypeName);
	Type?   GetOrAddClrType(string messageTypeName, Func<string, Type?> getClrType);
}

public class MessageTypeRegistry : IMessageTypeRegistry {
	readonly               ConcurrentDictionary<string, Type?> _typeMap     = new();
	readonly               ConcurrentDictionary<Type, string>  _typeNameMap = new();

	public void Map(Type messageType, string messageTypeName) {
		_typeNameMap.AddOrUpdate(messageType, messageTypeName, (_, typeName) => typeName);
		_typeMap.AddOrUpdate(messageTypeName, messageType, (_, type) => type);
	}

	public string? GetTypeName(Type messageType) =>
#if NET48
		_typeNameMap.TryGetValue(messageType, out var typeName) ? typeName : null;
#else
		_typeNameMap.GetValueOrDefault(messageType);
#endif

	public string GetOrAddTypeName(Type clrType, Func<Type, string> getTypeName) =>
		_typeNameMap.GetOrAdd(
			clrType,
			_ => {
				var typeName = getTypeName(clrType);

				_typeMap.TryAdd(typeName, clrType);

				return typeName;
			}
		);

	public Type? GetClrType(string messageTypeName) =>
#if NET48
		_typeMap.TryGetValue(messageTypeName, out var clrType) ? clrType : null;
#else
		_typeMap.GetValueOrDefault(messageTypeName);
#endif

	public Type? GetOrAddClrType(string messageTypeName, Func<string, Type?> getClrType) =>
		_typeMap.GetOrAdd(
			messageTypeName,
			_ => {
				var clrType = getClrType(messageTypeName);

				if (clrType == null)
					return null;

				_typeNameMap.TryAdd(clrType, messageTypeName);

				return clrType;
			}
		);
}

public static class MessageTypeMapperExtensions {
	public static void AddType<T>(this IMessageTypeRegistry messageTypeRegistry, string messageTypeName) =>
		messageTypeRegistry.Map(typeof(T), messageTypeName);

	public static void AddTypes(this IMessageTypeRegistry messageTypeRegistry, IDictionary<Type, string> typeMap) {
		foreach (var map in typeMap) {
			messageTypeRegistry.Map(map.Key, map.Value);
		}
	}

	public static string? GetTypeName<TMessageType>(this IMessageTypeRegistry messageTypeRegistry) =>
		messageTypeRegistry.GetTypeName(typeof(TMessageType));
}
