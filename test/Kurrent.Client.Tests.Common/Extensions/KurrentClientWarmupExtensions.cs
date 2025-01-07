using EventStore.Client;
using Grpc.Core;
using Polly;
using Polly.Contrib.WaitAndRetry;
using static System.TimeSpan;

namespace Kurrent.Client.Tests;

public static class KurrentClientWarmupExtensions {
	static readonly TimeSpan RediscoverTimeout = FromSeconds(5);

	/// <summary>
	/// max of 30 seconds (300 * 100ms)
	/// </summary>
	static readonly IEnumerable<TimeSpan> DefaultBackoffDelay = Backoff.ConstantBackoff(FromMilliseconds(100), 300);

	static async Task<T> TryWarmUp<T>(T client, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
		where T : KurrentClientBase {
		await Policy
			.Handle<RpcException>(ex => ex.StatusCode != StatusCode.Unimplemented)
			.Or<Exception>()
			.WaitAndRetryAsync(DefaultBackoffDelay)
			.WrapAsync(Policy.TimeoutAsync(RediscoverTimeout, (_, _, _) => client.RediscoverAsync()))
			.ExecuteAsync(
				async ct => {
					try {
						await action(ct).ConfigureAwait(false);
					}
					catch (Exception ex) when (ex is not OperationCanceledException) {
						// grpc throws a rpcexception when you cancel the token (which we convert into
						// invalid operation) - but polly expects operationcancelledexception or it wont
						// call onTimeoutAsync. so raise that here.
						ct.ThrowIfCancellationRequested();
						throw;
					}
				},
				cancellationToken
			);

		return client;
	}

	public static Task<KurrentClient> WarmUp(this KurrentClient client, CancellationToken cancellationToken = default) =>
		TryWarmUp(
			client,
			async ct => {
				// if we can read from $users then we know that
				// 1. the users exist
				// 2. we are connected to leader if we require it
				var users = await client
					.ReadStreamAsync(
						direction: Direction.Forwards,
						streamName: "$users",
						revision: StreamPosition.Start,
						maxCount: 1,
						userCredentials: TestCredentials.Root,
						cancellationToken: ct
					)
					.ToArrayAsync(ct);

				if (users.Length == 0)
					throw new("System is not ready yet...");

				// the read from leader above is not enough to guarantee the next write goes to leader
				_ = await client.AppendToStreamAsync(
					streamName: "warmup",
					expectedState: StreamState.Any,
					eventData: Enumerable.Empty<EventData>(),
					userCredentials: TestCredentials.Root,
					cancellationToken: ct
				);
			},
			cancellationToken
		);

	public static Task<KurrentOperationsClient> WarmUp(this KurrentOperationsClient client, CancellationToken cancellationToken = default) =>
		TryWarmUp(
			client,
			async ct => {
				await client.RestartPersistentSubscriptions(
					userCredentials: TestCredentials.Root,
					cancellationToken: ct
				);
			}, 
			cancellationToken
		);

	public static Task<KurrentPersistentSubscriptionsClient> WarmUp(
		this KurrentPersistentSubscriptionsClient client, CancellationToken cancellationToken = default
	) =>
		TryWarmUp(
			client,
			async ct => {
				var id = Guid.NewGuid();
				await client.CreateToStreamAsync(
					streamName: $"warmup-stream-{id}",
					groupName: $"warmup-group-{id}",
					settings: new(),
					userCredentials: TestCredentials.Root,
					cancellationToken: ct
				);
			},
			cancellationToken
		);

	public static Task<KurrentProjectionManagementClient> WarmUp(
		this KurrentProjectionManagementClient client, CancellationToken cancellationToken = default
	) =>
		TryWarmUp(
			client,
			async ct => {
				_ = await client
					.ListAllAsync(userCredentials: TestCredentials.Root, cancellationToken: ct)
					.Take(1)
					.ToArrayAsync(ct);

				// await client.RestartSubsystemAsync(userCredentials: TestCredentials.Root, cancellationToken: ct);
			},
			cancellationToken
		);

	public static Task<KurrentUserManagementClient> WarmUp(this KurrentUserManagementClient client, CancellationToken cancellationToken = default) =>
		TryWarmUp(
			client,
			async ct => {
				_ = await client
					.ListAllAsync(userCredentials: TestCredentials.Root, cancellationToken: ct)
					.Take(1)
					.ToArrayAsync(ct);
			},
			cancellationToken
		);
}
