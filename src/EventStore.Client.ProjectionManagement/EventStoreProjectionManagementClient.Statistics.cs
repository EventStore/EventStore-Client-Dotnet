using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		/// <summary>
		/// List the <see cref="ProjectionDetails"/> of all one-time projections.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectionDetails> ListOneTimeAsync(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
				OneTime = new Empty()
			}, userCredentials, cancellationToken);

		/// <summary>
		/// List the <see cref="ProjectionDetails"/> of all continuous projections.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectionDetails> ListContinuousAsync(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
				Continuous = new Empty()
			}, userCredentials, cancellationToken);

		/// <summary>
		/// Gets the status of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<ProjectionDetails?> GetStatusAsync(string name, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var result = await ListInternalAsync(new StatisticsReq.Types.Options {
				Name = name
			}, userCredentials, cancellationToken).ToArrayAsync(cancellationToken).ConfigureAwait(false);
			return result.FirstOrDefault();
		}

		private async IAsyncEnumerable<ProjectionDetails> ListInternalAsync(StatisticsReq.Types.Options options,
			UserCredentials? userCredentials,
			[EnumeratorCancellation] CancellationToken cancellationToken) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).Statistics(new StatisticsReq {
				Options = options
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));

			await foreach (var projectionDetails in call.ResponseStream
				.ReadAllAsync(cancellationToken)
				.Select(ConvertToProjectionDetails)
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false)) {
				yield return projectionDetails;
			}
		}

		private static ProjectionDetails ConvertToProjectionDetails(StatisticsResp response) {
			var details = response.Details;

			return new ProjectionDetails(details.CoreProcessingTime, details.Version, details.Epoch,
				details.EffectiveName, details.WritesInProgress, details.ReadsInProgress, details.PartitionsCached,
				details.Status, details.StateReason, details.Name, details.Mode, details.Position, details.Progress,
				details.LastCheckpoint, details.EventsProcessedAfterRestart, details.CheckpointStatus,
				details.BufferedEvents, details.WritePendingEventsBeforeCheckpoint,
				details.WritePendingEventsAfterCheckpoint);
		}

		/// <summary>
		/// List the <see cref="ProjectionDetails"/> of all projections.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectionDetails> ListAllAsync(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
				All = new Empty()
			}, userCredentials, cancellationToken);
	}
}
