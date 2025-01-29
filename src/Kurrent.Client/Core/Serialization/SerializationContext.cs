using System.Diagnostics.CodeAnalysis;
using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

using static ContentTypeExtensions;

public record SerializationContext(
	SchemaRegistry SchemaRegistry,
	ContentType DefaultContentType,
	AutomaticDeserialization AutomaticDeserialization
) {
	public EventData[] Serialize(IEnumerable<object> messages) {
		return Serialize(DefaultContentType, messages);
	}

	public EventData[] Serialize(ContentType contentType, IEnumerable<object> messages) {
		if (AutomaticDeserialization == AutomaticDeserialization.Disabled)
			throw new InvalidOperationException("Cannot serialize, automatic deserialization is disabled");

		var serializer = SchemaRegistry.GetSerializer(contentType);

		return messages.Select(serializer.Serialize).ToArray();
	}

#if NET48
	public bool TryDeserialize(EventRecord eventRecord, out object? deserialized) {
#else
	public bool TryDeserialize(EventRecord eventRecord, [NotNullWhen(true)] out object? deserialized) {
#endif
		if (AutomaticDeserialization == AutomaticDeserialization.Disabled) {
			deserialized = null;
			return false;
		}

		return SchemaRegistry
			.GetSerializer(FromMessageContentType(eventRecord.ContentType))
			.TryDeserialize(eventRecord, out deserialized);
	}

	public static SerializationContext From(KurrentClientSerializationSettings? settings = null) {
		settings ??= new KurrentClientSerializationSettings();

		return new SerializationContext(
			SchemaRegistry.From(settings),
			settings.DefaultContentType,
			settings.AutomaticDeserialization
		);
	}
}
