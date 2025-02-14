using EventStore.Client;

namespace Kurrent.Client.Streams.GettingState;

public record GetStateResult<TState>(
	TState State,
	StreamPosition? LastStreamPosition = null,
	Position? LastPosition = null
);

public class BuildStateOptions {
	/// <summary>
	/// The <see cref="EventStore.Client.StreamPosition"/> to start reading from.
	/// </summary>
	public StreamPosition FromStreamPosition { get; set; } = StreamPosition.Start;

	/// <summary>
	/// The <see cref="EventStore.Client.Position"/> to start reading from.
	/// </summary>
	public Position FromPosition { get; set; } = Position.Start;

	/// <summary>
	/// The <see cref="EventStore.Client.StreamPosition"/> to end reading on.
	/// </summary>
	public StreamPosition ToStreamPosition { get; set; } = StreamPosition.Start;

	/// <summary>
	/// The <see cref="EventStore.Client.Position"/> to end reading on.
	/// </summary>
	public Position ToPosition { get; set; } = Position.Start;

	/// <summary>
	/// The expected <see cref="ExpectedStreamState"/> of the stream to append to.
	/// </summary>
	public StreamState? ExpectedStreamState { get; set; }

	/// <summary>
	/// The expected <see cref="ExpectedStreamRevision"/> of the stream to append to.
	/// </summary>
	public StreamRevision? ExpectedStreamRevision { get; set; }

	/// <summary>
	/// The number of events to read
	/// </summary>
	public long MaxCount { get; set; } = long.MaxValue;

	/// <summary>
	/// Toggle this to fail when builder didn't get any messages (e.g. stream was empty)
	/// </summary>
	public bool ShouldFailOnNoMessages { get; set; }
}

public interface IStateBuilder<TState> {
	public Task<GetStateResult<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		BuildStateOptions options,
		CancellationToken ct = default
	);
}

public interface IState<in TEvent> {
	public void When(TEvent @event);
}

public static class StateBuilder {
	public static Task<GetStateResult<TState>> GetAsync<TState>(
		this IStateBuilder<TState> stateBuilder,
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct = default
	) => stateBuilder.GetAsync(messages, new BuildStateOptions(), ct);

	public static StateBuilder<TState, TEvent> From<TState, TEvent>()
		where TState : IState<TEvent>, new () =>
		new StateBuilder<TState, TEvent>(
			(state, @event) => {
				state.When(@event);
				return state;
			},
			() => new TState()
		);
	
	public static StateBuilder<TState, TEvent> From<TState, TEvent>(Func<TState> getInitialState)
		where TState : IState<TEvent> =>
		new StateBuilder<TState, TEvent>(
			(state, @event) => {
				state.When(@event);
				return state;
			},
			getInitialState
		);
}

public static class KurrentClientGettingStateExtensions {
	public static Task<GetStateResult<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		CancellationToken cancellationToken = default
	) =>
		eventStore.ReadStreamAsync(streamName, cancellationToken: cancellationToken)
			.GetStateAsync(stateBuilder, cancellationToken);
	
	public static Task<GetStateResult<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		CancellationToken cancellationToken = default
	) where TState: IState<TEvent>, new() =>
		eventStore.ReadStreamAsync(streamName, cancellationToken: cancellationToken)
			.GetStateAsync(StateBuilder.From<TState, TEvent>(), cancellationToken);

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
		BuildStateOptions options,
		CancellationToken ct
	) {
		var state = GetInitialState();

		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
				return new GetStateResult<TState>(state);
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
