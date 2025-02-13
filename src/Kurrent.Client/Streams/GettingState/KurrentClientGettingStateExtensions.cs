using EventStore.Client;

namespace Kurrent.Client.Streams.GettingState;

public record StateBuilder<TState, TEvent>(
	Func<TState, TEvent, TState> Evolve,
	Func<TState> GetInitialState
);

public record AsyncStateBuilder<TState, TEvent>(
	Func<TState, TEvent, TState> Evolve,
	Func<ValueTask<TState>> GetInitialState
) {
	public static AsyncStateBuilder<TState, TEvent> From(StateBuilder<TState, TEvent> stateBuilder) =>
		new AsyncStateBuilder<TState, TEvent>(
			stateBuilder.Evolve,
			() => new ValueTask<TState>(stateBuilder.GetInitialState())
		);
}

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

public static class KurrentClientGettingStateExtensions {
	public static async Task<TState?> GetState<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		AsyncStateBuilder<TState, TEvent> stateBuilder,
		CancellationToken cancellationToken = default
	) {
		var readResult = eventStore.ReadStreamAsync(
			streamName,
			cancellationToken: cancellationToken
		);

		var initialState = await stateBuilder.GetInitialState();

		if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
			return initialState;

		return await readResult
			.Select(e => e.DeserializedData)
			.OfType<TEvent>()
			.AggregateAsync(
				initialState,
				stateBuilder.Evolve,
				cancellationToken
			);
	}
}
