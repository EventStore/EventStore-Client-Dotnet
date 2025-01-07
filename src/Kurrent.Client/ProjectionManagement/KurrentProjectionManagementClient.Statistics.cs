using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;
using Grpc.Core;

namespace EventStore.Client {
	public partial class KurrentProjectionManagementClient {
		/// <summary>
		/// List the <see cref="ProjectionDetails"/> of all one-time projections.
		/// </summary>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectionDetails> ListOneTimeAsync(TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
					OneTime = new Empty()
				},
				KurrentCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken),
				cancellationToken);

		/// <summary>
		/// List the <see cref="ProjectionDetails"/> of all continuous projections.
		/// </summary>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectionDetails> ListContinuousAsync(TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
					Continuous = new Empty()
				},
				KurrentCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken),
				cancellationToken);

		/// <summary>
		/// Gets the status of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task<ProjectionDetails?> GetStatusAsync(string name,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) => ListInternalAsync(new StatisticsReq.Types.Options {
					Name = name
				},
				KurrentCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken),
				cancellationToken)
			.FirstOrDefaultAsync(cancellationToken).AsTask();

		/// <summary>
		/// List the <see cref="ProjectionDetails"/> of all projections.
		/// </summary>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectionDetails> ListAllAsync(TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
					All = new Empty()
				},
				KurrentCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken),
				cancellationToken);

		private async IAsyncEnumerable<ProjectionDetails> ListInternalAsync(StatisticsReq.Types.Options options,
			CallOptions callOptions,
			[EnumeratorCancellation] CancellationToken cancellationToken) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).Statistics(new StatisticsReq {
				Options = options
			}, callOptions);

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
	}
}
