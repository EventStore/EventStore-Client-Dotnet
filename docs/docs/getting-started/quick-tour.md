# Quick tour

This is a quick tour into the basic operations with EventStoreDb. We will look at creating a connection, writing an event and reading an event.

## Requirements

These examples have the following requirements:
- At least [.NET Core SDK 3.1](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started)
- A reference to the [EventStore.Client.Grpc](https://www.nuget.org/packages/EventStore.Client.Grpc/) NuGet package

## Setup the certificates and running the server

To run the EventStoreDB, create a new file called `docker-compose.yml` and copy the following contents into it.

<<< @/samples/quick-start/docker-compose.yml

Then run the command.

```bash
docker-compose up
```

This will launch a new instance of the EventStoreDb server .

## Creating a connection

Create a new console application.

The following example shows the simplest way to create a connection to EventStoreDb on your local machine.

<<< @/samples/quick-start/Program.cs#creating-connection

EventStoreDB runs secure by default. To avoid setting up any certificates we need to override the `HttpClientHandler` to tell it to trust all certificates.

::: tip
By default, the server listens to port 2113 for requests.
:::

## Writing an event

Writing an event to the database involves two stages. 

Firstly you have to construct a new `EventData` that contains a unique id, an event type, and a byte array containing the event data. Usually this is represented by json but can take any format.

Secondly you have to append that `EventData` to a stream. Making sure to specify the stream name, the expected state of the stream and then the data.

<<< @/samples/quick-start/Program.cs#append-to-stream

## Reading an event

You can read events from a stream in both directions. In this case we are reading the `some-stream` forwards from the start. We are reading a single event. 

This provides an `IAsyncEnumerable`, which you can then iterate on.
 
 <<< @/samples/quick-start/Program.cs#read-stream

