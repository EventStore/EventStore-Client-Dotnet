using EventStore.Client;

namespace Kurrent.Client.Streams.GettingState;

public record GetStateResult<TState>(
	TState State,
	StreamPosition? LastStreamPosition = null,
	Position? LastPosition = null
);

public interface IStateBuilder<TState> {
	public Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct = default
	);
}

public interface IState<in TEvent> {
	public void Apply(TEvent @event);
}

public static class BuildState {
	public static Task<GetStateResult<TState>> GetAsync<TState>(
		this IStateBuilder<TState> stateBuilder,
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(messages, ct);

	public static StateBuilder<TState, TEvent> From<TState, TEvent>()
		where TState : IState<TEvent>, new() =>
		new StateBuilder<TState, TEvent>(
			(state, @event) => {
				state.Apply(@event);
				return state;
			},
			() => new TState()
		);

	public static StateBuilder<TState, TEvent> From<TState, TEvent>(Func<TState> getInitialState)
		where TState : IState<TEvent> =>
		new StateBuilder<TState, TEvent>(
			(state, @event) => {
				state.Apply(@event);
				return state;
			},
			getInitialState
		);

	public static AsyncStateBuilder<TState, TEvent> From<TState, TEvent>(
		Func<ValueTask<TState>> getInitialState
	) where TState : IState<TEvent> =>
		new AsyncStateBuilder<TState, TEvent>(
			(state, @event) => {
				state.Apply(@event);
				return state;
			},
			getInitialState
		);
}

public static class KurrentClientGettingStateClientExtensions {
	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		eventStore.GetStateAsync(streamName, stateBuilder, new ReadStreamOptions(), cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		ReadStreamOptions options,
		CancellationToken cancellationToken = default
	) =>
		eventStore.ReadStreamAsync(streamName, options, cancellationToken)
			.GetStateAsync(stateBuilder, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		CancellationToken cancellationToken = default
	) where TState : IState<TEvent>, new() =>
		eventStore.GetStateAsync<TState, TEvent>(streamName, new ReadStreamOptions(), cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		ReadStreamOptions options,
		CancellationToken cancellationToken = default
	) where TState : IState<TEvent>, new() =>
		eventStore.ReadStreamAsync(streamName, options, cancellationToken)
			.GetStateAsync(BuildState.From<TState, TEvent>(), cancellationToken);
}

public static class KurrentClientGettingStateReadAndSubscribeExtensions {
	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadStreamResult readStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(readStreamResult, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadAllStreamResult readAllStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(readAllStreamResult, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.StreamSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, cancellationToken);
}

public record StateBuilder<TState, TEvent>(
	Func<TState, TEvent, TState> Evolve,
	Func<TState> GetInitialState
) : IStateBuilder<TState> {
	public virtual async Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct
	) {
		var state = GetInitialState();

		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.Ok)
				return new GetStateResult<TState>(state);
		}

		ResolvedEvent? lastEvent = null;

		await foreach (var resolvedEvent in messages.WithCancellation(ct)) {
			lastEvent = resolvedEvent;

			if (resolvedEvent.DeserializedData is not TEvent @event)
				continue;

			state = Evolve(state, @event);
		}

		return new GetStateResult<TState>(state, lastEvent?.Event.EventNumber, lastEvent?.Event.Position);
	}
}

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
