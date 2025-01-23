using EventStore.Client.Serialization;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Tests.Streams.Serialization;

namespace EventStore.Client {
	public partial class KurrentPersistentSubscriptionsClient {
		// TODO: Resolve based on options
		ISchemaSerializer _schemaSerializer = new SchemaSerializer(
			new SystemTextJsonSerializer(),
			EventTypeMapper.Instance
		);
	}
}
