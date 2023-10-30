using Grpc.Core;
using static System.Threading.Timeout;

namespace EventStore.Client;

static class EventStoreCallOptions {
	// deadline falls back to infinity
	public static CallOptions CreateStreaming(
		EventStoreClientSettings settings,
		TimeSpan? deadline = null, UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) =>
		Create(settings, deadline, userCredentials, cancellationToken);

	// deadline falls back to connection DefaultDeadline
	public static CallOptions CreateNonStreaming(
		EventStoreClientSettings settings,
		CancellationToken cancellationToken
	) =>
		Create(
			settings,
			settings.DefaultDeadline,
			settings.DefaultCredentials,
			cancellationToken
		);

	public static CallOptions CreateNonStreaming(
		EventStoreClientSettings settings, TimeSpan? deadline,
		UserCredentials? userCredentials, CancellationToken cancellationToken
	) =>
		Create(
			settings,
			deadline ?? settings.DefaultDeadline,
			userCredentials,
			cancellationToken
		);

	static CallOptions Create(
		EventStoreClientSettings settings, TimeSpan? deadline,
		UserCredentials? userCredentials, CancellationToken cancellationToken
	) =>
		new(
			cancellationToken: cancellationToken,
			deadline: DeadlineAfter(deadline),
			headers: new() {
				{
					Constants.Headers.RequiresLeader,
					settings.ConnectivitySettings.NodePreference == NodePreference.Leader
						? bool.TrueString
						: bool.FalseString
				}
			},
			credentials: (userCredentials ?? settings.DefaultCredentials) == null
				? null
				: CallCredentials.FromInterceptor(
					async (_, metadata) => {
						var credentials = userCredentials ?? settings.DefaultCredentials;

						var authorizationHeader = await settings.OperationOptions
							.GetAuthenticationHeaderValue(credentials!, CancellationToken.None)
							.ConfigureAwait(false);

						metadata.Add(Constants.Headers.Authorization, authorizationHeader);
					}
				)
		);

	static DateTime? DeadlineAfter(TimeSpan? timeoutAfter) =>
		!timeoutAfter.HasValue
			? new DateTime?()
			: timeoutAfter.Value == TimeSpan.MaxValue || timeoutAfter.Value == InfiniteTimeSpan
				? DateTime.MaxValue
				: DateTime.UtcNow.Add(timeoutAfter.Value);
}