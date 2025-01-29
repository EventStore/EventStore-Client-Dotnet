using System.Diagnostics.CodeAnalysis;
using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

using static ContentTypeExtensions;

public readonly struct Message {
	/// <summary>
	/// The raw bytes of the event data.
	/// </summary>
	public readonly object Data;

	/// <summary>
	/// The raw bytes of the event metadata.
	/// </summary>
	public readonly object? Metadata;

	/// <summary>
	/// The <see cref="Uuid"/> of the event, used as part of the idempotent write check.
	/// </summary>
	public readonly Uuid EventId;

	/// <summary>
	/// Constructs a new <see cref="Message"/>.
	/// </summary>
	/// <param name="data">The raw bytes of the event data.</param>
	/// <param name="metadata">The raw bytes of the event metadata.</param>
	/// <param name="eventId">The <see cref="Uuid"/> of the event, used as part of the idempotent write check.</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public Message(object data, object? metadata = null, Uuid? eventId = null) {
		if (eventId == Uuid.Empty) 
			throw new ArgumentOutOfRangeException(nameof(eventId));
		
		EventId    = eventId ?? Uuid.NewUuid();
		Data       = data;
		Metadata   = metadata;
	}

	public void Deconstruct(out object data, out object? metadata, out Uuid eventId) {
		data     = Data;
		metadata = Metadata;
		eventId  = EventId;
	}
}

public record MessageSerializationContext(
	string StreamName,
	ContentType? ContentType = null
) {
	public string CategoryName => 
		// TODO: This is dangerous, as separator can be changed in database settings
		StreamName.Split('-').FirstOrDefault() ?? "no_stream_category";
}

public record SerializationContext(
	SchemaRegistry SchemaRegistry,
	ContentType DefaultContentType,
	AutomaticDeserialization AutomaticDeserialization
) {
	public EventData[] Serialize(IEnumerable<Message> messages, MessageSerializationContext context) {
		if (AutomaticDeserialization == AutomaticDeserialization.Disabled)
			throw new InvalidOperationException("Cannot serialize, automatic deserialization is disabled");

		var serializer = SchemaRegistry.GetSerializer(context.ContentType ?? DefaultContentType);

		return messages.Select(m => serializer.Serialize(m, context)).ToArray();
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
