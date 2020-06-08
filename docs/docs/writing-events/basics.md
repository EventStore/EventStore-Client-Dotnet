# Writing Events

The simplest way to write an event to EventStoreDb is to create an `EventData` object and call `AppendToStreamAsync()`

@[code transcludeWith=//append-to-stream](@/samples/writing-events/Program.cs)

As you can see `AppendToStreamAsync()` a `IEnumerable<EventData>` so it is also possible to provide a collection of events to be saved in a single batch. 
 
As well as the example above there is also a number of other options for dealing with different scenarios. 

> If you are new to event sourcing its probably wise to pay special attention to handling concurrency that is detailed below.

## Working with EventData

When appending events to EventStoreDb they must first all be wrapped in an `EventData` object. This allow you to specify the content of the event, type of event whether its in Json format etc. In it's simplest form you need to the three following arguments.

### eventId

This takes the format of of a `Uuid` and is used to uniquely identify the event you are trying to append. If two events with the same `Uuid` are appended to the same stream in quick succession EventStoreDb will only append one copy of the event to the stream. 

For example:

@[code transcludeWith=//append-duplicate-event](@/samples/writing-events/Program.cs)

will result in only a single event being written

![Duplicate Event](images/duplicate-event.png)

For most scenarios you can just provide `Uuid.NewUuid()` although there are methods for generating a `Uuid` from other types if you need to change how this `Uuid` is generated

### type

An event type should be supplied for each event. This is a unique string to identify the type of event you are saving. 

It is common to see the CLR type used as the type as it makes serialising and de-serialising of the event easy. However we recommend against this as it couples the storage to the type and will make it more difficult if you need to version the event at a later date.

### data

A byte encoded representation of your event. It is recommended that you store your events as JSON objects as this will allow you to make use of all of EventStoreDb's functionality such as projections. Ultimately though, you can save it using whatever format you like.

### metadata

It is common to need to store additional information along side your event that is part of the event it's self. This can be correlation Id's, timestamps, access information etc. EventStoreDb allows you to store a separate byte array containing this information to keep it separate

### isJson

Simple boolean field to tell EventStoreDb if the event is stored as json, true by default

## Handling concurrency

Whe 


## Options
Throw on append failure etc

## user credentials
