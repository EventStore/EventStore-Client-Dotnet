using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace EventStore.Tests.Fixtures;

public class EventStoreClientIntegrationFixture : EventStoreIntegrationFixture {
	public EventStoreClient Client { get; private set; } = null!;

	protected override async Task OnInitialized() {
		Client = new EventStoreClient(Options.ClientSettings);


		//var Streams = new EventStoreStreamsClient(Options.ClientSettings);
		var Operations              = new EventStoreOperationsClient(Options.ClientSettings);
		var Users                   = new EventStoreUserManagementClient(Options.ClientSettings);
		var Projections             = new EventStoreProjectionManagementClient(Options.ClientSettings);
		var PersistentSubscriptions = new EventStorePersistentSubscriptionsClient(Options.ClientSettings);
		
		await Client.WarmUpAsync();

		//TODO SS: in order to migrate/refactor code faster will keep Given() and When() apis for now
		await Given().WithTimeout(TimeSpan.FromMinutes(5));
		await When().WithTimeout(TimeSpan.FromMinutes(5));
	}
	
	protected virtual Task Given() => Task.CompletedTask;
	protected virtual Task When() => Task.CompletedTask;

	public string GetStreamName([CallerMemberName] string? testMethod = null) {
		var type = GetType();
		return $"{type.DeclaringType?.Name}.{testMethod ?? "unknown"}";
	}
	
	public const string TestEventType = "-";
	
	public IEnumerable<EventData> CreateTestEvents(int count = 1, string? type = null, int metadataSize = 1) =>
		Enumerable.Range(0, count).Select(index => CreateTestEvent(index, type ?? TestEventType, metadataSize));

	protected static EventData CreateTestEvent(int index) => CreateTestEvent(index, TestEventType, 1);

	protected static EventData CreateTestEvent(int index, string type, int metadataSize) =>
		new EventData(
			eventId: Uuid.NewUuid(), 
			type: type, 
			data: Encoding.UTF8.GetBytes($@"{{""x"":{index}}}"),
			metadata: Encoding.UTF8.GetBytes($"\"{new string('$', metadataSize)}\""));
}
