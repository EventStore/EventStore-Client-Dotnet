# Using a custom HttpMessageHandler

:::tip
Packages needed:
- `EventStore.Client.Grpc.Streams`   
:::

The Event Store connection also allows you to override the [`HttpMessageHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler) used in the connection.

The example below show's how it can be used to trust all certificates.

<<< @/samples/connecting-to-a-single-node/Program.cs#adding-an-custom-http-message-handler
