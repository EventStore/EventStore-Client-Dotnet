using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Streams.GettingState;

public record StateAtPointInTime<TState>(
	TState State,
	StreamPosition? LastStreamPosition = null,
	Position? LastPosition = null
);

public interface IStateBuilder<TState> {
	public Task<StateAtPointInTime<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct = default
	);
}

public interface IState : IState<object>;

public interface IState<in TEvent> {
	public void Apply(TEvent @event);
}

// public record StateBuilder<TState, TEvent>(
// 	Func<TState, TEvent, TState> Evolve,
// 	Func<TState> GetInitialState
// ) : IStateBuilder<TState> {
// 	public Task<StateAtPointInTime<TState>> GetAsync(
// 		IAsyncEnumerable<ResolvedEvent> messages,
// 		CancellationToken ct
// 	) =>
// 		messages.GetState(
// 			GetInitialState(),
// 			(state, resolvedEvent) =>
// 				resolvedEvent.DeserializedData is TEvent @event ? Evolve(state, @event) : state,
// 			ct
// 		);
// }

public record StateBuilder<TState>(
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState,
	GetSnapshot<TState>? GetSnapshot = null
) : IStateBuilder<TState> {
	public Task<StateAtPointInTime<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		CancellationToken ct
	) =>
		messages.GetState(GetInitialState(), Evolve, ct);
}

public static class StateBuilder {
	public static StateBuilder<TState> For<TState, TEvent>(
		Func<TState, TEvent, TState> evolve,
		Func<TState> getInitialState,
		GetSnapshot<TState>? getSnapshot = null
	) =>
		new StateBuilder<TState>(
			(state, resolvedEvent) =>
				resolvedEvent.DeserializedData is TEvent @event
					? evolve(state, @event)
					: state,
			getInitialState,
			getSnapshot
		);

	public static StateBuilder<TState> For<TState>(
		Func<TState, object, TState> evolve,
		Func<TState> getInitialState,
		GetSnapshot<TState>? getSnapshot = null
	) =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => resolvedEvent.DeserializedData != null
				? evolve(state, resolvedEvent.DeserializedData)
				: state,
			getInitialState,
			getSnapshot
		);

	public static StateBuilder<TState> For<TState>(
		Func<TState, Message, TState> evolve,
		Func<TState> getInitialState,
		GetSnapshot<TState>? getSnapshot = null
	) =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => resolvedEvent.Message != null
				? evolve(state, resolvedEvent.Message)
				: state,
			getInitialState,
			getSnapshot
		);

	public static StateBuilder<TState> For<TState, TEvent>(
		GetSnapshot<TState>? getSnapshot = null
	)
		where TState : IState<TEvent>, new() =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData is TEvent @event)
					state.Apply(@event);

				return state;
			},
			() => new TState(),
			getSnapshot
		);

	public static StateBuilder<TState> For<TState>(
		GetSnapshot<TState>? getSnapshot = null
	)
		where TState : IState<object>, new() =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData != null)
					state.Apply(resolvedEvent.DeserializedData);

				return state;
			},
			() => new TState(),
			getSnapshot
		);

	public static StateBuilder<TState> For<TState, TEvent>(
		Func<TState> getInitialState,
		GetSnapshot<TState>? getSnapshot = null
	)
		where TState : IState<TEvent> =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData is TEvent @event)
					state.Apply(@event);

				return state;
			},
			getInitialState,
			getSnapshot
		);

	public static StateBuilder<TState> For<TState>(
		Func<TState> getInitialState,
		GetSnapshot<TState>? getSnapshot = null
	)
		where TState : IState<object>, new() =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData != null)
					state.Apply(resolvedEvent.DeserializedData);

				return state;
			},
			getInitialState,
			getSnapshot
		);

	public static async Task<StateAtPointInTime<TState>> GetState<TState>(
		this IAsyncEnumerable<ResolvedEvent> messages,
		TState initialState,
		Func<TState, ResolvedEvent, TState> evolve,
		CancellationToken ct
	) {
		var state = initialState;

		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
				return new StateAtPointInTime<TState>(state);
		}

		ResolvedEvent? lastEvent = null;

		await foreach (var resolvedEvent in messages.WithCancellation(ct)) {
			lastEvent = resolvedEvent;

			state = evolve(state, resolvedEvent);
		}

		return new StateAtPointInTime<TState>(state, lastEvent?.Event.EventNumber, lastEvent?.Event.Position);
	}
}

public record GetSnapshotOptions {
	public string? StreamName { get; set; }

	public static GetSnapshotOptions ForStream(string streamName) =>
		new GetSnapshotOptions { StreamName = streamName };

	public static GetSnapshotOptions ForAll() =>
		new GetSnapshotOptions();
}

public delegate ValueTask<StateAtPointInTime<TState>> GetSnapshot<TState>(
	GetSnapshotOptions options,
	CancellationToken ct = default
);

public static class KurrentClientGettingStateClientExtensions {
	public static async Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		StateBuilder<TState> streamStateBuilder,
		ReadStreamOptions options,
		CancellationToken ct = default
	) {
		StateAtPointInTime<TState>? stateAtPointInTime = null;
		var                         stateBuilder       = streamStateBuilder;

		if (streamStateBuilder.GetSnapshot != null) {
			stateAtPointInTime = await streamStateBuilder.GetSnapshot(
				GetSnapshotOptions.ForStream(streamName),
				ct
			);

			stateBuilder = stateBuilder with { GetInitialState = () => stateAtPointInTime.State };
		}

		options.StreamPosition = stateAtPointInTime?.LastStreamPosition ?? StreamPosition.Start;

		return await eventStore.ReadStreamAsync(streamName, options, ct)
			.GetStateAsync(stateBuilder, ct);
	}

	public static async Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		StateBuilder<TState> streamStateBuilder,
		ReadAllOptions options,
		CancellationToken ct = default
	) {
		StateAtPointInTime<TState>? stateAtPointInTime = null;
		var                         stateBuilder       = streamStateBuilder;

		if (streamStateBuilder.GetSnapshot != null) {
			stateAtPointInTime = await streamStateBuilder.GetSnapshot(GetSnapshotOptions.ForAll(), ct);

			stateBuilder = stateBuilder with { GetInitialState = () => stateAtPointInTime.State };
		}

		options.Position = stateAtPointInTime?.LastPosition ?? Position.Start;

		return await eventStore.ReadAllAsync(options, ct)
			.GetStateAsync(stateBuilder, ct);
	}

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		eventStore.ReadStreamAsync(streamName, new ReadStreamOptions(), ct)
			.GetStateAsync(stateBuilder, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		ReadStreamOptions options,
		CancellationToken ct = default
	) =>
		eventStore.ReadStreamAsync(streamName, options, ct)
			.GetStateAsync(stateBuilder, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.GetStateAsync<TState, TEvent>(streamName, new ReadStreamOptions(), ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		ReadStreamOptions options,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.ReadStreamAsync(streamName, options, ct)
			.GetStateAsync(StateBuilder.For<TState, TEvent>(), ct);
}

public static class KurrentClientGettingStateReadAndSubscribeExtensions {
	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadStreamResult readStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(readStreamResult, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadAllStreamResult readAllStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(readAllStreamResult, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.StreamSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) =>
		stateBuilder.GetAsync(subscriptionResult, ct);
}
