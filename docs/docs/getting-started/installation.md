# Installation

## Requirements

All NuGet packages require .NET Core SDK 3.1.

## Installation

The client consists of several packages, each which expose different functionality depending on what you need your application to do
These are:

| Package | Purpose |
|:------ |:------- |
| `EventStore.Client.Grpc ` | The base gRPC client library for EventStoreDB. You shouldn't need to install this |
| `EventStore.Client.Grpc.Streams` | Contains methods for appending and reading from streams |   
| `EventStore.Client.Grpc.PersistentSubcriptions` | Allows managing and subscribing to persistent subscriptions |   
| `EventStore.Client.Grpc.ProjectionManagement` | Allows the creation and management of projections |
| `EventStore.Client.Grpc.Operations` | Contains methods for initiating a scavenge and other operations |
| `EventStore.Client.Grpc.UserManagement` | Contains methods for user management, such as creating and removing |

::: tip
This guide will note which packages you'd needed at the beginning of each section
:::
