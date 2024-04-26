# EventStoreDB .NET Client

EventStoreDB is the event-native database, where business events are immutably stored and streamed. Designed for event-sourced, event-driven, and microservices architectures

This is the repository for the .NET client for EventStoreDB version 20+ and uses gRPC as the communication protocol.
If you're looking for the TCP client library (legacy), check the [EventStoreDB-Client-Dotnet-Legacy](https://github.com/EventStore/EventStoreDB-Client-Dotnet-Legacy) repo.

## Installation

Reference the nuget package(s) for the API that you would like to call

[Streams](https://www.nuget.org/packages/EventStore.Client.Grpc.Streams)

[Operations](https://www.nuget.org/packages/EventStore.Client.Grpc.Operations)

[Persistent Subscriptions](https://www.nuget.org/packages/EventStore.Client.Grpc.PersistentSubscriptions)

[Projection Management](https://www.nuget.org/packages/EventStore.Client.Grpc.ProjectionManagement)

[User Management](https://www.nuget.org/packages/EventStore.Client.Grpc.UserManagement)

## Open Telemetry

Telemetry instrumentation can be enabled by installing the [Open Telemetry Extensions](https://www.nuget.org/packages/EventStore.Client.Extensions.OpenTelemetry) package.

Once installed you can configure instrumentation using the `AddEventStoreClientInstrumentation` extension method on a `TracerProviderBuilder`.

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    ...
    .AddEventStoreClientInstrumentation()
    ...
    .Build();
```

Tracing is the only telemetry currently exported, specifically for the `Append` and `Subscribe` (Catchup and Persistent) operations.

For more information about Open Telemetry, refer to the [official documentation](https://opentelemetry.io/docs/what-is-opentelemetry/).

## Support

Information on support and commercial tools such as LDAP authentication can be found here: [Event Store Support](https://eventstore.com/support/).

## CI Status

![Build](https://github.com/EventStore/EventStore-Client-Dotnet/actions/workflows/ci.yml/badge.svg)
![Build](https://github.com/EventStore/EventStore-Client-Dotnet/actions/workflows/lts.yml/badge.svg)
![Build](https://github.com/EventStore/EventStore-Client-Dotnet/actions/workflows/previous-lts.yml/badge.svg)

## Documentation

Documentation for EventStoreDB can be found here: [Event Store Docs](https://eventstore.com/docs/).

Bear in mind that this client is not yet properly documented. We are working hard on a new version of the documentation.

## Communities

- [Discuss](https://discuss.eventstore.com/)
- [Discord (Event Store)](https://discord.gg/Phn9pmCw3t)
- [Discord (ddd-cqrs-es)](https://discord.com/invite/sEZGSHNNbH)

## Contributing

Development is done on the `master` branch.
We attempt to do our best to ensure that the history remains clean and to do so, we generally ask contributors to squash their commits into a set or single logical commit.
