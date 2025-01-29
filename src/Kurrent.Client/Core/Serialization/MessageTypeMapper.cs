using System.Collections.Concurrent;

namespace Kurrent.Client.Tests.Streams.Serialization;

// TODO: Discuss how to proceed with that and whether to move the Schema Registry code here
// The scanning part and registration seems to be more robust there
// I used this for simplicity
public interface IMessageTypeMapper {
	void    AddType<T>(string eventTypeName);
	void    AddType(Type eventType, string eventTypeName);
	string? GetTypeName<TEventType>();
	string? GetTypeName(Type eventType);
	Type?   GetClrType(string eventTypeName);
}

public class MessageTypeMapper : IMessageTypeMapper {
	public static readonly MessageTypeMapper                   Instance     = new MessageTypeMapper();
	readonly               ConcurrentDictionary<string, Type?> _typeMap     = new();
	readonly               ConcurrentDictionary<Type, string>  _typeNameMap = new();

	public void AddType<T>(string eventTypeName) => AddType(typeof(T), eventTypeName);

	public void AddType(Type eventType, string eventTypeName) {
		_typeNameMap.AddOrUpdate(eventType, eventTypeName, (_, typeName) => typeName);
		_typeMap.AddOrUpdate(eventTypeName, eventType, (_, type) => type);
	}

	public string? GetTypeName<TEventType>() => GetTypeName(typeof(TEventType));

	public string? GetTypeName(Type eventType) =>
#if NET48
		_typeNameMap.TryGetValue(eventType, out var typeName) ? typeName : null;
#else
		_typeNameMap.GetValueOrDefault(eventType);
#endif

	public Type? GetClrType(string eventTypeName) =>
#if NET48
		_typeMap.TryGetValue(eventTypeName, out var clrType) ? clrType : null;
#else
		_typeMap.GetValueOrDefault(eventTypeName);
#endif

	public Type? GetClrTypeOrAdd(string eventTypeName, Func<string, Type?> getClrType) =>
		_typeMap.GetOrAdd(eventTypeName, getClrType);

	// _ => {
	// 	var type = TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly(eventTypeName);
	//
	// 	if (type == null)
	// 		return null;
	//
	// 	_typeNameMap.TryAdd(type, eventTypeName);
	//
	// 	return type;
	// }
	// );
}
