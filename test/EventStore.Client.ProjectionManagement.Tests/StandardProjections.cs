namespace EventStore.Client;

static class StandardProjections {
    public static readonly string[] Names = {
        "$streams",
        "$stream_by_category",
        "$by_category",
        "$by_event_type",
        "$by_correlation_id"
    };

    public static Task Created(EventStoreProjectionManagementClient client) {
        var systemProjectionsReady = Names.Select(
            async name => {
                var ready = false;

                while (!ready) {
                    var result = await client.GetStatusAsync(name, userCredentials: TestCredentials.Root);

                    if (result?.Status.Contains("Running") ?? false)
                        ready = true;
                    else
                        await Task.Delay(100);
                }
            }
        );

        return Task.WhenAll(systemProjectionsReady);
    }
}