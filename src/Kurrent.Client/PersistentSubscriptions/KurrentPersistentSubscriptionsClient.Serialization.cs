using Kurrent.Client.Core.Serialization;

namespace EventStore.Client {
	public partial class KurrentPersistentSubscriptionsClient {
		readonly MessageSerializerWrapper _messageSerializer;
	}
}
