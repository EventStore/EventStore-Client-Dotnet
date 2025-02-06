using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

public record Message(object Data, object? Metadata, Uuid? EventId = null) {
	public static Message From(object data, Uuid eventId) => 
		From(data, null, eventId);
	
	public static Message From(object data, object? metadata = null, Uuid? eventId = null) {
		if (eventId == Uuid.Empty)
			throw new ArgumentOutOfRangeException(nameof(eventId));

		return new Message(data, metadata, eventId);
	}
}
