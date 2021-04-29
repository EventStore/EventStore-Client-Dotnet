using System;
using System.Threading;
using Grpc.Core;
using Timeout_ = System.Threading.Timeout;
#nullable enable
namespace EventStore.Client {
	internal static class EventStoreCallOptions {
		public static CallOptions Create(EventStoreClientSettings settings,
			EventStoreClientOperationOptions operationOptions, UserCredentials? userCredentials,
			CancellationToken cancellationToken) => new(
			cancellationToken: cancellationToken,
			deadline: DeadlineAfter(operationOptions.TimeoutAfter),
			headers: new Metadata(),
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
