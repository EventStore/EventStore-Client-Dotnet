using EventStore.Client;

namespace Kurrent.Client.Streams.GettingState;

public record GetStateResult<TState>(
	TState State,
	StreamPosition? LastStreamPosition = null,
	Position? LastPosition = null
) {
	public bool StreamExists => LastStreamPosition != null;
}

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
	public static StateBuilder<TState, TEvent> From<TState, TEvent>(
		Func<TState, TEvent, TState> evolve,
		Func<TState> getInitialState
	) =>
		new StateBuilder<TState, TEvent>(evolve, getInitialState);

	public static AsyncStateBuilder<TState, TEvent> From<TState, TEvent>(
		Func<TState, TEvent, TState> evolve,
		Func<ValueTask<TState>> getInitialState
	) =>
		new AsyncStateBuilder<TState, TEvent>(evolve, getInitialState);

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

	public static async Task<GetStateResult<TState>> GetState<TState>(
		this IAsyncEnumerable<ResolvedEvent> messages,
		TState initialState,
		Func<TState, ResolvedEvent, TState> evolve,
		CancellationToken ct
	) {
		var state = initialState;

		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.Ok)
				return new GetStateResult<TState>(state);
		}

		ResolvedEvent? lastEvent = null;

		await foreach (var resolvedEvent in messages.WithCancellation(ct)) {
			lastEvent = resolvedEvent;

			state = evolve(state, resolvedEvent);
		}

		return new GetStateResult<TState>(state, lastEvent?.Event.EventNumber, lastEvent?.Event.Position);
	}
}

public static class KurrentClientGettingStateClientExtensions {
	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		eventStore.GetStateAsync(streamName, stateBuilder, new ReadStreamOptions(), ct);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		ReadStreamOptions options,
		CancellationToken ct = default
	) =>
		eventStore.ReadStreamAsync(streamName, options, ct)
			.GetStateAsync(stateBuilder, ct);

	public static Task<GetStateResult<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.GetStateAsync<TState, TEvent>(streamName, new ReadStreamOptions(), ct);

	public static Task<GetStateResult<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		ReadStreamOptions options,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.ReadStreamAsync(streamName, options, ct)
			.GetStateAsync(BuildState.From<TState, TEvent>(), ct);
}

public static class KurrentClientGettingStateReadAndSubscribeExtensions {
	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadStreamResult readStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(readStreamResult, ct);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadAllStreamResult readAllStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(readAllStreamResult, ct);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.StreamSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, ct);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, ct);
}

public record StateBuilder<TState>(
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState
) : IStateBuilder<TState> {
	public Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct
	) =>
		messages.GetState(GetInitialState(), Evolve, ct);
}

public record StateBuilder<TState, TEvent>(
	Func<TState, TEvent, TState> Evolve,
	Func<TState> GetInitialState
) : IStateBuilder<TState> {
	public Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct
	) =>
		messages.GetState(
			GetInitialState(),
			(state, resolvedEvent) =>
				resolvedEvent.DeserializedData is TEvent @event ? Evolve(state, @event): state,
			ct
		);
}

public record AsyncStateBuilder<TState>(
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<ValueTask<TState>> GetInitialState
) : IStateBuilder<TState> {
	public async Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct
	) =>
		await messages.GetState(
			await GetInitialState(),
			Evolve,
			ct
		);
}

public record AsyncStateBuilder<TState, TEvent>(
	Func<TState, TEvent, TState> Evolve,
	Func<ValueTask<TState>> GetInitialState
) : IStateBuilder<TState> {
	public async Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct
	) =>
		await messages.GetState(
			await GetInitialState(),
			(state, resolvedEvent) =>
				resolvedEvent.DeserializedData is TEvent @event ? Evolve(state, @event): state,
			ct
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
