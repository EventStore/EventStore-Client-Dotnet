# The basics

There are two options for reading events from EventStoreDB. You can either read from an individual stream or you can read from the `$all` stream. The later will return all events in the store.

All events in EventStoreDB belong to an individual stream name. When reading events you can pick the name of the stream you want to read from. Then you can choose whether to read that stream forwards or backwards. 

All events have a `StreamPosition`, that is it's place in the stream, represented by a `ulong` and a `Position` that is the events logical position that is represented by `CommitPosition` and a `PreparePosition`. This means that when reading events you have to supply a different "position" depending on if you are reading from a stream or the `$all` stream.
