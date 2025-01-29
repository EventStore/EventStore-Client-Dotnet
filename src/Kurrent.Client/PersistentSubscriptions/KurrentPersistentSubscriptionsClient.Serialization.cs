using Kurrent.Client.Core.Serialization;

namespace EventStore.Client {
	public partial class KurrentPersistentSubscriptionsClient {
		readonly SchemaRegistry _schemaRegistry;
		readonly SerializationContext _defaultSerializationContext;
	}
}
