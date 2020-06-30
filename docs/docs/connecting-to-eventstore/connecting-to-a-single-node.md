# Connecting to a single node

The simplest way to connect to EventStoreDB is to only use a single node and leave the rest of the options as default. 

<<< @/samples/connecting-to-a-single-node/Program.cs#creating-simple-connection

## Setting the connection name

If you have multiple clients connecting to Event Store it is advisable to set the connection name so that it can be easily identified via the UI. This is done via a property on the `EventStoreClientSettings`.

<<< @/samples/connecting-to-a-single-node/Program.cs#setting-the-connection-name

This name will then show in the Admin UI allowing you to identify which clients are connected to the server.

:::warning
There is currently an issue with gRPC clients not appearing in the clients list in the UI
:::

## Overriding the request timeout

The default timeout for a single request is **5 seconds** but this can be modified on the connectivity settings.

<<< @/samples/connecting-to-a-single-node/Program.cs#overriding-timeout

:::tip
Subscriptions are not bound by the timeout as they are long-lived
:::

## Username and password



