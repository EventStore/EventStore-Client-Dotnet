# Setting up a connection with .NET dependency injection

:::tip
Packages needed:
- `EventStore.Client.Grpc.Streams`   
- `EventStore.Client.Grpc.ProjectionManagement`
- `EventStore.Client.Grpc.Operations`
- `EventStore.Client.Grpc.UserManagement`
:::

As part of the client packages provide `IServiceCollection` extension methods to make it easy to setup dependency injection in .NET Core web applications. 

All the same methods and options are available and can be set while registering the dependency during startup:

<<< @/samples/setting-up-dependency-injection/Startup.cs#setting-up-dependency

The `EventStoreClient` can then be referenced in controllers as follows:

<<< @/samples/setting-up-dependency-injection/Controllers/EventStoreController.cs#using-dependency

All packages have their own extension methods:

| Package | Extension method |
|:------ |:------- |
| `EventStore.Client.Grpc.Streams` | `AddEventStoreClient()` |   
| `EventStore.Client.Grpc.PersistentSubcriptions` | `AddEventStorePersistentSubscriptionsClient()` |   
| `EventStore.Client.Grpc.ProjectionManagement` | `AddEventStoreProjectionManagementClient()` |
| `EventStore.Client.Grpc.Operations` | `AddEventStoreOperationsClient()` |
| `EventStore.Client.Grpc.UserManagement` | `AddEventStoreUserManagementClient()` |
