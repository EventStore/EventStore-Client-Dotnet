using EventStore.Client;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public record Decider<TState, TCommand, TEvent>(
	Func<TCommand, TState, TEvent[]> Decide,
	Func<TState, TEvent, TState> Evolve,
	Func<TState> GetInitialState
) : StateBuilder<TState, TEvent>(Evolve, GetInitialState);

public record AsyncDecider<TState, TCommand, TEvent>(
	Func<TCommand, TState, ValueTask<TEvent[]>> Decide,
	Func<TState, TEvent, TState> Evolve,
	Func<ValueTask<TState>> GetInitialState
) : AsyncStateBuilder<TState, TEvent>(Evolve, GetInitialState) {
	public static AsyncDecider<TState, TCommand, TEvent> From(Decider<TState, TCommand, TEvent> decider) =>
		new AsyncDecider<TState, TCommand, TEvent>(
			(command, state) => new ValueTask<TEvent[]>(decider.Decide(command, state)),
			decider.Evolve,
			() => new ValueTask<TState>(decider.GetInitialState())
		);
}

public static class KurrentClientDecisionMakingClientExtensions {
	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		eventStore.GetStateAsync(streamName, stateBuilder, new ReadStreamOptions(), ct);

	// public static Task GetAndUpdate<T, TEvent>(
	// 	this KurrentClient eventStore,
	// 	Func<T> getInitial,
	// 	Guid id,
	// 	Action<T> handle,
	// 	CancellationToken ct
	// )
	// 	where T : Aggregate<TEvent>
	// 	where TEvent: notnull =>
	// 	eventStore.GetAndUpdate<T, TEvent>(
	// 		(state, @event) =>
	// 		{
	// 			state.Evolve(@event);
	// 			return state;
	// 		},
	// 		getInitial,
	// 		id,
	// 		state =>
	// 		{
	// 			handle(state);
	// 			var events = state.DequeueUncommittedEvents();
	// 			return events;
	// 		}, ct);

	public static async Task GetAndUpdate<TState, TCommand, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		Decider<TState, TCommand, TEvent> decider,
		CancellationToken ct
	) {
		var (state, streamPosition, _) = await eventStore.GetStateAsync(streamName, decider, ct);

		var events = decider.Decide(command, state);

		var options = streamPosition.HasValue
			? new AppendToStreamOptions
				{ ExpectedStreamRevision = StreamRevision.FromStreamPosition(streamPosition.Value) }
			: new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream };

		await eventStore.AppendToStreamAsync(
			streamName,
			events.Cast<object>(),
			options,
			cancellationToken: ct
		);
	}

// 	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
// 		this KurrentClient eventStore,
// 		string streamName,
// 		IStateBuilder<TState> stateBuilder,
// 		ReadStreamOptions options,
// 		CancellationToken ct = default
// 	) =>
// 		eventStore.ReadStreamAsync(streamName, options, ct)
// 			.GetStateAsync(stateBuilder, ct);
}
