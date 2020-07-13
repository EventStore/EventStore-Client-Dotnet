# Reading from the all stream

Reading from the all stream is similar to reading from an individual stream but with some small differences. Primarily the need to provide an admin user account credentials and that you need to to provide a transaction log position instead of a stream revision.

## Reading forwards

The simplest way to read the `$all` stream forwards is to supply a direction and transaction log position to start from. This can either be a `Position.Start` or a `ulong`:

<<< @/samples/reading-events/Program.cs#read-from-all-stream

This will return an AsyncEnumerable that can be iterated on:

<<< @/samples/reading-events/Program.cs#iterate-all-stream

There are a number of additional arguments you can provide when reading a stream

#
