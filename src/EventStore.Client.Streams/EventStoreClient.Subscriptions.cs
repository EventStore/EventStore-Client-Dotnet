using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
		private Task<StreamSubscription> SubscribeToAllAsync(
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			EventStoreClientOperationOptions operationOptions,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			operationOptions.TimeoutAfter = DeadLine.None;

			return StreamSubscription.Confirm(ReadInternal(new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
						ResolveLinks = resolveLinkTos,
						All = new ReadReq.Types.Options.Types.AllOptions {
							Start = new Empty()
						},
						Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
						Filter = GetFilterOptions(filterOptions)!
					}
				}, operationOptions, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, _log,
				filterOptions?.CheckpointReached, cancellationToken);
		}

		/// <summary>
		/// Subscribes to all events. Use this when you have no <see cref="Position">checkpoint</see>.
		/// </summary>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToAllAsync(
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			SubscriptionFilterOptions? filterOptions = null,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return SubscribeToAllAsync(eventAppeared, operationOptions, resolveLinkTos, subscriptionDropped,
				filterOptions, userCredentials, cancellationToken);
		}

		private Task<StreamSubscription> SubscribeToAllAsync(Position start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			EventStoreClientOperationOptions operationOptions,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			operationOptions.TimeoutAfter = DeadLine.None;

			return StreamSubscription.Confirm(ReadInternal(new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
						ResolveLinks = resolveLinkTos,
						All = ReadReq.Types.Options.Types.AllOptions.FromPosition(start),
						Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
						Filter = GetFilterOptions(filterOptions)!
					}
				}, operationOptions, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, _log,
				filterOptions?.CheckpointReached, cancellationToken);
		}

		/// <summary>
		/// Subscribes to all events from a <see cref="Position">checkpoint</see>. This is exclusive of.
		/// </summary>
		/// <param name="start">A <see cref="Position"/> (exclusive of) to start the subscription from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToAllAsync(Position start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			SubscriptionFilterOptions? filterOptions = null,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return SubscribeToAllAsync(start, eventAppeared, operationOptions, resolveLinkTos, subscriptionDropped,
				filterOptions, userCredentials, cancellationToken);
		}

		private Task<StreamSubscription> SubscribeToStreamAsync(string streamName,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			EventStoreClientOperationOptions operationOptions,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			operationOptions.TimeoutAfter = DeadLine.None;

			return StreamSubscription.Confirm(ReadInternal(new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
						ResolveLinks = resolveLinkTos,
						Stream = new ReadReq.Types.Options.Types.StreamOptions {
							Start = new Empty(),
							StreamIdentifier = streamName
						},
						Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions()
					}
				}, operationOptions, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, _log,
				cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Subscribes to all events in a stream. Use this when you have no <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToStreamAsync(string streamName,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return SubscribeToStreamAsync(streamName, eventAppeared, operationOptions, resolveLinkTos,
				subscriptionDropped,
				userCredentials, cancellationToken);
		}

		private Task<StreamSubscription> SubscribeToStreamAsync(string streamName,
			StreamPosition start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			EventStoreClientOperationOptions operationOptions,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			operationOptions.TimeoutAfter = DeadLine.None;

			return StreamSubscription.Confirm(ReadInternal(new ReadReq {
						Options = new ReadReq.Types.Options {
							ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
							ResolveLinks = resolveLinkTos,
							Stream = start == StreamPosition.End
								? new ReadReq.Types.Options.Types.StreamOptions {
									End = new Empty(),
									StreamIdentifier = streamName
								}
								: new ReadReq.Types.Options.Types.StreamOptions {
									Revision = start,
									StreamIdentifier = streamName
								},
							Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions()
						}
					},
					operationOptions, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, _log,
				 cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>. This is exclusive of.
		/// </summary>
		/// <param name="start">A <see cref="Position"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToStreamAsync(string streamName,
			StreamPosition start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return SubscribeToStreamAsync(streamName, start, eventAppeared, operationOptions, resolveLinkTos,
				subscriptionDropped, userCredentials, cancellationToken);
		}
	}
}
