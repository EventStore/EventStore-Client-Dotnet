# Reading from the $all stream

Reading from the all stream is similar to reading from an individual stream but with some small differences. Primarily the need to provide an admin user account credentials and that you need to to provide a transaction log position instead of a stream revision.

## Reading forwards

The simplest way to read the `$all` stream forwards is to supply a direction and transaction log position to start from. This can either be a `Position.Start` or a `ulong`:

<<< @/samples/reading-events/Program.cs#read-from-all-stream

This will return an AsyncEnumerable that can be iterated on:

<<< @/samples/reading-events/Program.cs#read-from-all-stream-iterate

There are a number of additional arguments you can provide when reading a stream.

### maxCount

Passing in the max count allows you to limit the number of events that returned. 

### resolveLinkTos

When using projections to create new events you can set whether the generated events are pointers to existing events. Setting this value to true will tell EventStoreDB to return the event as well as the event linking to it.

### configureOperationOptions

This argument is generic setting class for all operations that can be set on all operations executed against EventStoreDB. To find out more see [here]().

### userCredentials
The credentials used to read the data can be supplied. Please see [here](authentication/authenticating-with-username-password.html).
 for more information.
 
## Reading backwards

As well as being able to read a stream forwards you can also go backwards. When reading backwards is the the `Position` will have to be set to the end if you want to read all the events:

<<< @/samples/reading-events/Program.cs#read-from-all-stream-backwards 

:::tip
You can use reading backwards to find the last position in the stream. Just read backwards one event and get the position.
:::

## Handling system events

When reading from the all stream EventStoreDB will also return system events. In most cases you won't need to care about these events and they should be ignored.

All system events begin with `$` or `$$` and can be easily ignored by checking the `EventType` property.

<<< @/samples/reading-events/Program.cs#ignore-system-events
 
## Understanding the $all stream position
TODO





