using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="start">A <see cref="FromAll"/> (exclusive of) to start the subscription from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToAllAsync(
			FromAll start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) => StreamSubscription.Confirm(ReadInternal(new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					All = ReadReq.Types.Options.Types.AllOptions.FromSubscriptionPosition(start),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
					Filter = GetFilterOptions(filterOptions)!
				}
			}, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, _log,
			filterOptions?.CheckpointReached, cancellationToken);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToStreamAsync(string streamName,
			FromStream start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) => StreamSubscription.Confirm(ReadInternal(new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					Stream = ReadReq.Types.Options.Types.StreamOptions.FromSubscriptionPosition(streamName, start),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions()
				}
			}, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, _log,
			cancellationToken: cancellationToken);
	}
}
