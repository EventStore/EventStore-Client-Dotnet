using EventStore.Client.Serialization;

namespace EventStore.Client {
	public partial class KurrentClient {
		readonly IMessageSerializer _messageSerializer;
	}
}
