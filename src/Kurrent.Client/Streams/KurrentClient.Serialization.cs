using Kurrent.Client.Core.Serialization;

namespace EventStore.Client {
	public partial class KurrentClient {
		readonly SchemaRegistry         _schemaRegistry;
		readonly DeserializationContext _defaultDeserializationContext;
	}
}
