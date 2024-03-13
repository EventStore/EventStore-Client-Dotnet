using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;

namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		/// <summary>
		/// Enables a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task EnableAsync(string name, TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(userCredentials, cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).EnableAsync(new EnableReq {
				Options = new EnableReq.Types.Options {
					Name = name
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Resets a projection. This will re-emit events. Streams that are written to from the projection will also be soft deleted.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task ResetAsync(string name, TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(userCredentials, cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).ResetAsync(new ResetReq {
				Options = new ResetReq.Types.Options {
					Name = name,
					WriteCheckpoint = true
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Aborts a projection. Does not save the projection's checkpoint.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task AbortAsync(string name, TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			DisableInternalAsync(name, false, deadline, userCredentials, cancellationToken);

		/// <summary>
		/// Disables a projection. Saves the projection's checkpoint.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task DisableAsync(string name, TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			DisableInternalAsync(name, true, deadline, userCredentials, cancellationToken);

		/// <summary>
		/// Restarts the projection subsystem.
		/// </summary>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task RestartSubsystemAsync(TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(userCredentials, cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).RestartSubsystemAsync(new Empty(),
				EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}
		
		private async Task DisableInternalAsync(string name, bool writeCheckpoint, TimeSpan? deadline,
			UserCredentials? userCredentials, CancellationToken cancellationToken) {
			var channelInfo = await GetChannelInfo(userCredentials, cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).DisableAsync(new DisableReq {
				Options = new DisableReq.Types.Options {
					Name = name,
					WriteCheckpoint = writeCheckpoint
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
