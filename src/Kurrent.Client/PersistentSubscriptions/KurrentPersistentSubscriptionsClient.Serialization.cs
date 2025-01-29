using EventStore.Client.Serialization;

namespace EventStore.Client {
	public partial class KurrentPersistentSubscriptionsClient {
		readonly IMessageSerializer _messageSerializer;
	}
}
