using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;

namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		/// <summary>
		/// Creates a one-time projection.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateOneTimeAsync(string query, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					OneTime = new Empty(),
					Query = query
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a continuous projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="query"></param>
		/// <param name="trackEmittedStreams"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Continuous = new CreateReq.Types.Options.Types.Continuous {
						Name = name,
						TrackEmittedStreams = trackEmittedStreams
					},
					Query = query
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a transient projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="query"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateTransientAsync(string name, string query, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Transient = new CreateReq.Types.Options.Types.Transient {
						Name = name
					},
					Query = query
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
