# Server-side filtering

EventStoreDB allows you to filter the events whilst you subscribe to the `$all` stream so that you only receive the events that you care about.

You can filter by event type or stream name using either a regular expression or a prefix. Server-side filtering is currently only available on the `$all` stream.

:::tip
Server-side filtering introduced as a simpler alternative to projections. Before creating a projection to get the events you care about you should first consider filtering.
:::

## Filtering out system events

There are a number of events in EventStoreDB called system events. These are prefixed with a `$` and under most circumstances you won't care about these. The can befiltered out by passing in a `SubscriptionFilterOptions` when subscribing to the all stream.

<<< @/samples/server-side-filtering/Program.cs#exclude-system

:::tip
`$stats` events are no longer stored in EventStoreDB by default so there won't be as many `$` events as before.
:::

## Filtering by event type

If you only want to subscribe to events of a given type there are two options. You can either use a regular expression or a prefix.

### Filtering by prefix

If you want to filter by prefix pass in a `SubscriptionFilterOptions` to the subscription with an `EventTypeFilter.Prefix`.

<<< @/samples/server-side-filtering/Program.cs#event-type-prefix

This will only subscribe to events with a type that begin with `customer-`.

### Filtering by regular expression

If you want to subscribe to multiple event types then it might be better to provide a regular expression.

<<< @/samples/server-side-filtering/Program.cs#event-type-regex

This will subscribe to any event that begins with `user` or `company`.

## Filtering by stream name

If you only want to subscribe to streams with a given there are two options. You can either use a regular expression or a prefix.

### Filtering by prefix

If you want to filter by prefix pass in a `SubscriptionFilterOptions` to the subscription with an `StreamFilter.Prefix`.

<<< @/samples/server-side-filtering/Program.cs#stream-prefix

This will only subscribe to streams with a name that begin with `user-`.

### Filtering by regular expression

If you want to subscribe to multiple streams then it might be better to provide a regular expression.

<<< @/samples/server-side-filtering/Program.cs#stream-regex

This will subscribe to any stream with a name that begins with `account` or `savings`.

## Checkpointing
There is one thing to consider with server-side filtering, and that is when events that match your filter are few and far between. In this scenario, you might find yourself in the situation where EventStoreDB has searched through 1 million events and the last thing you want to happen is for the server to get to event 900k and then have your client crash. It won't have been able to take a checkpoint and upon restart, you'd have to go back to the beginning and start again.

In this case you can make use of an additional delegate that will be triggered every n number of events (32 by default).

To make use of it set up `checkpointReached` on the `SubscriptionFilterOptions` class.

<<< @/samples/server-side-filtering/Program.cs#checkpoint

 This will be called every n number of events. If you want to be specific about the number of events threshold you can also pass that in.

<<< @/samples/server-side-filtering/Program.cs#checkpoint-with-interval

:::warning
This number will be called every n * 32 events.
:::
