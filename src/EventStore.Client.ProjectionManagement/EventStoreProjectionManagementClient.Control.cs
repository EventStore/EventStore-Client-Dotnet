using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		public async Task EnableAsync(string name, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			using var call = _client.EnableAsync(new EnableReq {
				Options = new EnableReq.Types.Options {
					Name = name
				}
			}, RequestMetadata.Create(userCredentials ?? Settings.DefaultCredentials), cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);
		}

		public Task AbortAsync(string name, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			DisableInternalAsync(name, true, userCredentials, cancellationToken);

		public Task DisableAsync(string name, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			DisableInternalAsync(name, false, userCredentials, cancellationToken);

		private async Task DisableInternalAsync(string name, bool writeCheckpoint, UserCredentials? userCredentials,
			CancellationToken cancellationToken) {
			using var call = _client.DisableAsync(new DisableReq {
				Options = new DisableReq.Types.Options {
					Name = name,
					WriteCheckpoint = writeCheckpoint
				}
			}, RequestMetadata.Create(userCredentials ?? Settings.DefaultCredentials), cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);
		}

		public async Task RestartSubsystemAsync(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.RestartSubsystemAsync(new Empty(), RequestMetadata.Create(userCredentials ?? Settings.DefaultCredentials),
				cancellationToken: cancellationToken);
		}
	}
}
