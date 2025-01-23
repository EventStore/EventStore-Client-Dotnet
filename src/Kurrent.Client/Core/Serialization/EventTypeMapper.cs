using System.Collections.Concurrent;

namespace Kurrent.Client.Tests.Streams.Serialization;

// TODO: Discuss how to proceed with that and whether to move the Schema Registry code here
// The scanning part and registration seems to be more robust there
// I used this for simplicity
public interface IEventTypeMapper {
	void   AddCustomMap<T>(string eventTypeName);
	void   AddCustomMap(Type eventType, string eventTypeName);
	string ToName<TEventType>();
	string ToName(Type eventType);
	Type?  ToType(string eventTypeName);
}

public class EventTypeMapper : IEventTypeMapper {
	public static readonly EventTypeMapper Instance = new();

	private readonly ConcurrentDictionary<string, Type?> typeMap     = new();
	private readonly ConcurrentDictionary<Type, string>  typeNameMap = new();

	public void AddCustomMap<T>(string eventTypeName) => AddCustomMap(typeof(T), eventTypeName);

	public void AddCustomMap(Type eventType, string eventTypeName)
	{
		typeNameMap.AddOrUpdate(eventType, eventTypeName, (_, typeName) => typeName);
		typeMap.AddOrUpdate(eventTypeName, eventType, (_,     type) => type);
	}

	public string ToName<TEventType>() => ToName(typeof(TEventType));

	public string ToName(Type eventType) => typeNameMap.GetOrAdd(eventType, _ =>
	{
		var eventTypeName = eventType.FullName!;

		typeMap.TryAdd(eventTypeName, eventType);

		return eventTypeName;
	});

	public Type? ToType(string eventTypeName) => typeMap.GetOrAdd(eventTypeName, _ =>
	{
		var type = TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly(eventTypeName);

		if (type == null)
			return null;

		typeNameMap.TryAdd(type, eventTypeName);

		return type;
	});
}
