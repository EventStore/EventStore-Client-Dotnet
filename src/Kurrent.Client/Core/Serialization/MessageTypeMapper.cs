using System.Collections.Concurrent;

namespace Kurrent.Client.Tests.Streams.Serialization;

// TODO: Discuss how to proceed with that and whether to move the Schema Registry code here
// The scanning part and registration seems to be more robust there
// I used this for simplicity
public interface IMessageTypeMapper {
	void    AddType<T>(string messageTypeName);
	void    AddType(Type messageType, string eventTypeName);
	string? GetTypeName<TEventType>();
	string? GetTypeName(Type messageType);
	string  GetOrAddTypeName(Type clrType, Func<Type, string> getTypeName);
	Type?   GetClrType(string messageTypeName);
	Type?   GetOrAddClrType(string messageTypeName, Func<string, Type?> getClrType);
}

public class MessageTypeMapper : IMessageTypeMapper {
	public static readonly MessageTypeMapper                   Instance     = new MessageTypeMapper();
	readonly               ConcurrentDictionary<string, Type?> _typeMap     = new();
	readonly               ConcurrentDictionary<Type, string>  _typeNameMap = new();

	public void AddType<T>(string messageTypeName) => AddType(typeof(T), messageTypeName);

	public void AddType(Type messageType, string eventTypeName) {
		_typeNameMap.AddOrUpdate(messageType, eventTypeName, (_, typeName) => typeName);
		_typeMap.AddOrUpdate(eventTypeName, messageType, (_, type) => type);
	}

	public string? GetTypeName<TMessageType>() => GetTypeName(typeof(TMessageType));

	public string? GetTypeName(Type messageType) =>
#if NET48
		_typeNameMap.TryGetValue(messageType, out var typeName) ? typeName : null;
#else
		_typeNameMap.GetValueOrDefault(messageType);
#endif
	
	public string GetOrAddTypeName(Type clrType, Func<Type, string> getTypeName) =>
		_typeNameMap.GetOrAdd(clrType,
			_ => {
				var typeName = getTypeName(clrType);
				
				_typeMap.TryAdd(typeName, clrType);

				return typeName;
			});
	

	public Type? GetClrType(string messageTypeName) =>
#if NET48
		_typeMap.TryGetValue(messageTypeName, out var clrType) ? clrType : null;
#else
		_typeMap.GetValueOrDefault(messageTypeName);
#endif

	public Type? GetOrAddClrType(string messageTypeName, Func<string, Type?> getClrType) =>
		_typeMap.GetOrAdd(messageTypeName,
			_ => {
				var clrType = getClrType(messageTypeName);

				if (clrType == null)
					return null;
				
				_typeNameMap.TryAdd(clrType, messageTypeName);

				return clrType;
			});
}
