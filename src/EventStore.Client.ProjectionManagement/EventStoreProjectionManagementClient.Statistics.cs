using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		public IAsyncEnumerable<ProjectionDetails> ListOneTimeAsync(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
				OneTime = new Empty()
			}, userCredentials, cancellationToken);

		public IAsyncEnumerable<ProjectionDetails> ListContinuousAsync(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
				Continuous = new Empty()
			}, userCredentials, cancellationToken);

		public Task<ProjectionDetails> GetStatusAsync(string name, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
				Name = name
			}, userCredentials, cancellationToken).FirstOrDefaultAsync(cancellationToken).AsTask();

		private async IAsyncEnumerable<ProjectionDetails> ListInternalAsync(StatisticsReq.Types.Options options,
			UserCredentials? userCredentials,
			[EnumeratorCancellation] CancellationToken cancellationToken) {
			using var call = _client.Statistics(new StatisticsReq {
				Options = options
			}, RequestMetadata.Create(userCredentials));

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

		public IAsyncEnumerable<ProjectionDetails> ListAllAsync(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			ListInternalAsync(new StatisticsReq.Types.Options {
				All = new Empty()
			}, userCredentials, cancellationToken);
	}
}
