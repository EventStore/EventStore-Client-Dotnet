namespace EventStore.Client.Tests;

public static class EventStoreClientWarmupExtensions {
    public static Task WarmUp(this EventStoreClient client) =>
        DatabaseWarmup<EventStoreClient>.TryExecuteOnce(client, async ct => {
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
                    cancellationToken: ct)
                .ToArrayAsync(ct);

            if (users.Length == 0)
                throw new ("System is not ready yet...");

            // the read from leader above is not enough to guarantee the next write goes to leader
            _ = await client.AppendToStreamAsync(
                streamName: "warmup", 
                expectedState: StreamState.Any, 
                eventData: Enumerable.Empty<EventData>(),
                userCredentials: TestCredentials.Root,
                cancellationToken: ct
            );
        });

    public static Task WarmUp(this EventStoreOperationsClient client) =>
        DatabaseWarmup<EventStoreOperationsClient>.TryExecuteOnce(
            client,
            async ct => {
                await client.RestartPersistentSubscriptions(
                    userCredentials: TestCredentials.Root,
                    cancellationToken: ct
                );
            }
        );

    public static Task WarmUp(this EventStorePersistentSubscriptionsClient client) =>
        DatabaseWarmup<EventStorePersistentSubscriptionsClient>.TryExecuteOnce(
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
            }
        );

    public static Task WarmUp(this EventStoreProjectionManagementClient client) =>
        DatabaseWarmup<EventStoreProjectionManagementClient>.TryExecuteOnce(
            client,
            async ct => {
                _ = await client
                    .ListAllAsync(
                        userCredentials: TestCredentials.Root,
                        cancellationToken: ct
                    )
                    .Take(1)
                    .ToArrayAsync(ct);

               // await client.RestartSubsystemAsync(userCredentials: TestCredentials.Root, cancellationToken: ct);
            }
        );

    public static Task WarmUp(this EventStoreUserManagementClient client) =>
        DatabaseWarmup<EventStoreUserManagementClient>.TryExecuteOnce(
            client,
            async ct => _ = await client
                .ListAllAsync(
                    userCredentials: TestCredentials.Root, 
                    cancellationToken: ct
                )
                .Take(1)
                .ToArrayAsync(ct)
        );
}