# Basics of connecting to Event Store

The simplest way to connect to Event Store is to only use a single node and leave the rest of the options as default. 

<<< @/samples/connecting/Program.cs#creating-simple-connection

## Setting the connection name

If you have multiple clients connecting to Event Store it is advisable to set the connection name so that it can be easily identified via the UI. This is done via a property on the `EventStoreClientSettings`.

<<< @/samples/connecting/Program.cs#setting-the-connection-name

This name will then show in the Admin UI allowing you to identify which clients are connected to the server.

> TODO: Figure out what is going on with gRPC connections

## Overriding the request timeout

The default timeout for a single request is **5 seconds** but this can be modified on the connectivity settings.

<<< @/samples/connecting/Program.cs#overriding-timeout

:::tip
Subscriptions are not bound by the timeout as they are long-lived.
:::

## Adding a gRPC interceptor

gRPC clients allow you to create [Interceptors](https://grpc.github.io/grpc/csharp/api/Grpc.Core.Interceptors.Interceptor.html) that can intercept the gRPC requests. 

The EventStoreDB client allows you to add one of these to the connection that can be used for logging all requests made or other such things. 

To create one, add a new class that inherits from `Interceptor` and override the methods that you want to intercept.

<<< @/samples/connecting/DemoInterceptor.cs#interceptor

Then add this to the array of interceptors on the `EventStoreClientSettings` class.

<<< @/samples/connecting/Program.cs#adding-an-interceptor

## Using a custom HttpMessageHandler

The Event Store connection also allows you to override the [`HttpMessageHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler) used in the connection.

<<< @/samples/connecting/Program.cs#adding-an-custom-http-message-handler



