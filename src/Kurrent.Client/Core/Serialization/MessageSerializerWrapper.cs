using System.Diagnostics.CodeAnalysis;
using EventStore.Client;
using EventStore.Client.Serialization;

namespace Kurrent.Client.Core.Serialization;

public class MessageSerializerWrapper(
	IMessageSerializer messageSerializer,
	AutomaticDeserialization automaticDeserialization
): IMessageSerializer {
	public EventData Serialize(Message value, MessageSerializationContext context) {
		if (automaticDeserialization == AutomaticDeserialization.Disabled)
			throw new InvalidOperationException("Cannot serialize, automatic deserialization is disabled");

		return messageSerializer.Serialize(value, context);
	}

#if NET48
	public bool TryDeserialize(EventRecord eventRecord, out object? deserialized) {
#else
	public bool TryDeserialize(EventRecord eventRecord, [NotNullWhen(true)] out object? deserialized) {
#endif
		if (automaticDeserialization == AutomaticDeserialization.Disabled) {
			deserialized = null;
			return false;
		}

		return messageSerializer
			.TryDeserialize(eventRecord, out deserialized);
	}

	public static MessageSerializerWrapper From(KurrentClientSerializationSettings? settings = null) {
		settings ??= new KurrentClientSerializationSettings();

		return new MessageSerializerWrapper(
			new MessageSerializer(SchemaRegistry.From(settings)),
			settings.AutomaticDeserialization
		);
	}
}
