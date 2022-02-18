using System;
using System.Threading;
using Grpc.Core;
using Timeout_ = System.Threading.Timeout;

#nullable enable
namespace EventStore.Client {
	internal static class EventStoreCallOptions {
		// deadline falls back to infinity
		public static CallOptions CreateStreaming(EventStoreClientSettings settings,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			Create(settings, deadline, userCredentials, cancellationToken);

		// deadline falls back to connection DefaultDeadline
		public static CallOptions CreateNonStreaming(EventStoreClientSettings settings, TimeSpan? deadline,
			UserCredentials? userCredentials, CancellationToken cancellationToken) => Create(settings,
			deadline ?? settings.DefaultDeadline, userCredentials, cancellationToken);

		private static CallOptions Create(EventStoreClientSettings settings, TimeSpan? deadline,
			UserCredentials? userCredentials, CancellationToken cancellationToken) => new(
			cancellationToken: cancellationToken,
			deadline: DeadlineAfter(deadline),
			headers: new Metadata {
				{
					Constants.Headers.RequiresLeader,
					settings.ConnectivitySettings.NodePreference == NodePreference.Leader
						? bool.TrueString
						: bool.FalseString
				}
			},
			credentials: (settings.DefaultCredentials ?? userCredentials) == null
				? null
				: CallCredentials.FromInterceptor(async (_, metadata) => {
					var credentials = settings.DefaultCredentials ?? userCredentials;

					var authorizationHeader = await settings.OperationOptions
						.GetAuthenticationHeaderValue(credentials!, CancellationToken.None)
						.ConfigureAwait(false);
					metadata.Add(Constants.Headers.Authorization, authorizationHeader);
				})
		);

		private static DateTime? DeadlineAfter(TimeSpan? timeoutAfter) => !timeoutAfter.HasValue
			? new DateTime?()
			: timeoutAfter.Value == TimeSpan.MaxValue || timeoutAfter.Value == Timeout_.InfiniteTimeSpan
				? DateTime.MaxValue
				: DateTime.UtcNow.Add(timeoutAfter.Value);
	}
}
