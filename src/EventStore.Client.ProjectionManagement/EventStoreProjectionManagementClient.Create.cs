using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		public async Task CreateOneTimeAsync(string query, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			using var call = _client.CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					OneTime = new Empty(),
					Query = query
				}
			}, RequestMetadata.Create(userCredentials), cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);
		}

		public async Task CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			using var call = _client.CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Continuous = new CreateReq.Types.Options.Types.Continuous {
						Name = name,
						TrackEmittedStreams = trackEmittedStreams
					},
					Query = query
				}
			}, RequestMetadata.Create(userCredentials), cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);
		}

		public async Task CreateTransientAsync(string name, string query, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			using var call = _client.CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Transient = new CreateReq.Types.Options.Types.Transient {
						Name = name
					},
					Query = query
				}
			}, RequestMetadata.Create(userCredentials), cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
