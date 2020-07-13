# Reading from a stream

When using EventStoreDB it's possible to read events from a given stream.

## Reading forwards

The simplest way to read a stream forwards is to supply a stream name, direction and revision to start from. This can either be a `StreamPosition.Start` or a `ulong`:

<<< @/samples/reading-events/Program.cs#read-from-stream

This will return an [`AsyncEnumerable`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=dotnet-plat-ext-3.1) that can be iterated on:

<<< @/samples/reading-events/Program.cs#iterate-stream
 
There are a number of additional arguments you can provide when reading a stream

### maxCount

Passing in the max count allows you to limit the number of events that returned. 

### resolveLinkTos

When using projections to create new events you can set whether the generated events are pointers to existing events. Setting this value to true will tell EventStoreDB to return the event as well as the event linking to it.

### configureOperationOptions

This argument is generic setting class for all operations that can be set on all operations executed against EventStoreDB. To find out more see [here]()

## Reading from a revision

As well as providing a `StreamPosition` you can also provide a specific stream revision in the form of a `ulong`

<<< @/samples/reading-events/Program.cs#read-from-stream-position

## Reading backwards

As well as being able to read a stream forwards you can also go backwards. One of the things to be aware of when reading backwards is the the `StreamPosition` will have to be set to the end if you want to read all the events:

<<< @/samples/reading-events/Program.cs#reading-backwards 

:::tip
You can use reading backwards to find the last position in the stream. Just read backwards one event and get the position.
:::
