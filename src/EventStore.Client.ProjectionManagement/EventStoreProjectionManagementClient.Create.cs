using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		/// <summary>
		/// Creates a one-time projection.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateOneTimeAsync(string query, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			using var call = new Projections.Projections.ProjectionsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					OneTime = new Empty(),
					Query = query
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a continuous projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="query"></param>
		/// <param name="trackEmittedStreams"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			using var call = new Projections.Projections.ProjectionsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Continuous = new CreateReq.Types.Options.Types.Continuous {
						Name = name,
						TrackEmittedStreams = trackEmittedStreams
					},
					Query = query
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a transient projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="query"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateTransientAsync(string name, string query, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			using var call = new Projections.Projections.ProjectionsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Transient = new CreateReq.Types.Options.Types.Transient {
						Name = name
					},
					Query = query
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
