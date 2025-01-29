using Kurrent.Client.Core.Serialization;

namespace EventStore.Client {
	public partial class KurrentClient {
		readonly MessageSerializerWrapper _messageSerializer;
	}
}
