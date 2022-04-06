using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Operations;

namespace EventStore.Client {
	public partial class EventStoreOperationsClient {
		/// <summary>
		/// Starts a scavenge operation.
		/// </summary>
		/// <param name="threadCount"></param>
		/// <param name="startFromChunk"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task<DatabaseScavengeResult> StartScavengeAsync(
			int threadCount = 1,
			int startFromChunk = 0,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (threadCount <= 0) {
				throw new ArgumentOutOfRangeException(nameof(threadCount));
			}

			if (startFromChunk < 0) {
				throw new ArgumentOutOfRangeException(nameof(startFromChunk));
			}

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Operations.Operations.OperationsClient(
				channelInfo.CallInvoker).StartScavengeAsync(
				new StartScavengeReq {
					Options = new StartScavengeReq.Types.Options {
						ThreadCount = threadCount,
						StartFromChunk = startFromChunk
					}
				},
				EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			var result = await call.ResponseAsync.ConfigureAwait(false);

			return result.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started => DatabaseScavengeResult.Started(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.Stopped => DatabaseScavengeResult.Stopped(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.InProgress => DatabaseScavengeResult.InProgress(result.ScavengeId),
				_ => DatabaseScavengeResult.Unknown(result.ScavengeId)
			};
		}

		/// <summary>
		/// Stops a scavenge operation.
		/// </summary>
		/// <param name="scavengeId"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<DatabaseScavengeResult> StopScavengeAsync(
			string scavengeId,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			var result = await new Operations.Operations.OperationsClient(
				channelInfo.CallInvoker).StopScavengeAsync(new StopScavengeReq {
				Options = new StopScavengeReq.Types.Options {
					ScavengeId = scavengeId
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken))
				.ResponseAsync.ConfigureAwait(false);

			return result.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started => DatabaseScavengeResult.Started(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.Stopped => DatabaseScavengeResult.Stopped(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.InProgress => DatabaseScavengeResult.InProgress(result.ScavengeId),
				_ => DatabaseScavengeResult.Unknown(result.ScavengeId)
			};
		}
	}
}
