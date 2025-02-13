using EventStore.Client;

namespace Kurrent.Client.Core.Serialization;

public record Message(object Data, object? Metadata, Uuid? MessageId = null) {
	public static Message From(object data, Uuid messageId) => 
		From(data, null, messageId);
	
	public static Message From(object data, object? metadata = null, Uuid? messageId = null) {
		if (messageId == Uuid.Empty)
			throw new ArgumentOutOfRangeException(nameof(messageId));

		return new Message(data, metadata, messageId);
	}
}
