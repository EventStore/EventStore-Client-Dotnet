using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Operations;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreOperationsClient {
		/// <summary>
		/// Starts a scavenge operation.
		/// </summary>
		/// <param name="threadCount"></param>
		/// <param name="startFromChunk"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task<DatabaseScavengeResult> StartScavengeAsync(
			int threadCount = 1,
			int startFromChunk = 0,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (threadCount <= 0) {
				throw new ArgumentOutOfRangeException(nameof(threadCount));
			}

			if (startFromChunk < 0) {
				throw new ArgumentOutOfRangeException(nameof(startFromChunk));
			}

			var result = await _client.StartScavengeAsync(new StartScavengeReq {
				Options = new StartScavengeReq.Types.Options {
					ThreadCount = threadCount,
					StartFromChunk = startFromChunk
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));

			return result.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started => DatabaseScavengeResult.Started(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.Stopped => DatabaseScavengeResult.Stopped(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.InProgress => DatabaseScavengeResult.InProgress(result.ScavengeId),
				_ => throw new InvalidOperationException()
			};
		}

		/// <summary>
		/// Stops a scavenge operation.
		/// </summary>
		/// <param name="scavengeId"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<DatabaseScavengeResult> StopScavengeAsync(
			string scavengeId,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var result = await _client.StopScavengeAsync(new StopScavengeReq {
				Options = new StopScavengeReq.Types.Options {
					ScavengeId = scavengeId
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));

			return result.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started => DatabaseScavengeResult.Started(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.Stopped => DatabaseScavengeResult.Stopped(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.InProgress => DatabaseScavengeResult.InProgress(result.ScavengeId),
				_ => throw new InvalidOperationException()
			};
		}
	}
}
