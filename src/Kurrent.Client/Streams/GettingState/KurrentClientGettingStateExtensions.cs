using EventStore.Client;

namespace Kurrent.Client.Streams.GettingState;

public record GetStateResult<TState>(TState State, StreamPosition? LastStreamPosition, Position? lastPosition);

public interface IBuildState<TState> {
	public Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct = default
	);
}

public static class KurrentClientGettingStateExtensions {
	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IBuildState<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		eventStore.ReadStreamAsync(streamName, cancellationToken: cancellationToken)
			.GetStateAsync(stateBuilder, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadStreamResult readStreamResult,
		IBuildState<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(readStreamResult, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadAllStreamResult readAllStreamResult,
		IBuildState<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(readAllStreamResult, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient.StreamSubscriptionResult subscriptionResult,
		IBuildState<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, cancellationToken);

	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult subscriptionResult,
		IBuildState<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, cancellationToken);
}

public record StateBuilder<TState, TEvent>(
	Func<TState, TEvent, TState> Evolve,
	Func<TState> GetInitialState
) : IBuildState<TState> {
	public virtual async Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct
	) {
		var state = GetInitialState();

		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
				return new GetStateResult<TState>(state, null, null);
		}

		StreamPosition? lastStreamPosition = null;
		Position?       lastPosition       = null;

		await foreach (var resolvedEvent in messages.WithCancellation(ct)) {
			lastStreamPosition = resolvedEvent.Event.EventNumber;
			lastPosition       = resolvedEvent.Event.Position;

			if (resolvedEvent.DeserializedData is not TEvent @event)
				continue;

			state = Evolve(state, @event);
		}

		return new GetStateResult<TState>(state, lastStreamPosition, lastPosition);
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
