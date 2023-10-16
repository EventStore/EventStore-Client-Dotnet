using System.Threading.Tasks;
using EventStore.Client;

namespace EventStore.Tests.Fixtures;

public class EventStoreUserManagementFixture : EventStoreIntegrationFixture {
	public EventStoreUserManagementClient Client { get; private set; } = null!;
	public EventStoreClient StreamsClient { get; private set; } = null!;

	protected override async Task OnInitialized() {
		Client = new EventStoreUserManagementClient(Options.ClientSettings);
		StreamsClient = new EventStoreClient(Options.ClientSettings);
		
		await Client.WarmUpAsync();
		await StreamsClient.WarmUpAsync();
	}
}

public class NoCredentialsEventStoreIntegrationFixture : EventStoreUserManagementFixture {
	protected override EsTestDbOptions Override(EsTestDbOptions options) {
		options.ClientSettings.DefaultCredentials = null;
		return options;
	}
}
