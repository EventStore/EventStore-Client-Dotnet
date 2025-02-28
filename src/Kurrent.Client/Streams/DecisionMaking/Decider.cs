using EventStore.Client;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public delegate ValueTask<Message[]> CommandHandler<in TState>(TState state, CancellationToken ct = default);

public interface ICommandHandler<in TState> {
	ValueTask<Message[]> Handle(TState state, CancellationToken ct = default);
}

public record AsyncDecider<TState, TCommand>(
	Func<TCommand, TState, CancellationToken, ValueTask<Message[]>> Decide,
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState,
	GetSnapshot<TState>? GetSnapshot = null
) : StateBuilder<TState>(
	Evolve,
	GetInitialState,
	GetSnapshot
);

public record Decider<TState, TCommand, TEvent>(
	Func<TCommand, TState, TEvent[]> Decide,
	Func<TState, TEvent, TState> Evolve,
	Func<TState> GetInitialState
) {
	public AsyncDecider<TState, TCommand> ToAsyncDecider(GetSnapshot<TState>? getSnapshot = null) =>
		new AsyncDecider<TState, TCommand>(
			(command, state, _) =>
				new ValueTask<Message[]>(Decide(command, state).Select(m => Message.From(m!)).ToArray()),
			(state, resolvedEvent) =>
				resolvedEvent.DeserializedData is TEvent @event
					? Evolve(state, @event)
					: state,
			GetInitialState,
			getSnapshot
		);
}

public record Decider<TState, TCommand>(
	Func<TCommand, TState, object[]> Decide,
	Func<TState, object, TState> Evolve,
	Func<TState> GetInitialState
) : Decider<TState, TCommand, object>(Decide, Evolve, GetInitialState);


public static class KurrentClientDecisionMakingClientExtensions {
	public static Task Decide<TState, TCommand>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		Decider<TState, TCommand> decider,
		CancellationToken ct
	) =>
		eventStore.Decide(
			streamName,
			command,
			decider.ToAsyncDecider(),
			ct
		);
	
	public static Task Decide<TState, TCommand>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		AsyncDecider<TState, TCommand> asyncDecider,
		CancellationToken ct
	) =>
		eventStore.Decide(
			streamName,
			(state, token) => asyncDecider.Decide(command, state, token),
			asyncDecider,
			ct
		);

	public static async Task Decide<TState>(
		this KurrentClient eventStore,
		string streamName,
		CommandHandler<TState> decide,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) {
		var (state, streamPosition, _) = await eventStore.GetStateAsync(streamName, stateBuilder, ct);

		var events = await decide(state, ct);

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
}
