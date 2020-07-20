using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The client used to manage persistent subscriptions in the EventStoreDB.
	/// </summary>
	public partial class EventStorePersistentSubscriptionsClient : EventStoreClientBase {
		private readonly PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient _client;
		private readonly ILogger _log;

		/// <summary>
		/// Constructs a new <see cref="EventStorePersistentSubscriptionsClient"/>.
		/// </summary>
		/// <param name="settings"></param>
		public EventStorePersistentSubscriptionsClient(EventStoreClientSettings? settings) : base(settings,
			new Dictionary<string, Func<RpcException, Exception>> {
				[Constants.Exceptions.PersistentSubscriptionDoesNotExist] = ex => new
					PersistentSubscriptionNotFoundException(
						ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
						ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex),
				[Constants.Exceptions.MaximumSubscribersReached] = ex => new
					MaximumSubscribersReachedException(
						ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
						ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex),
				[Constants.Exceptions.PersistentSubscriptionDropped] = ex => new
					PersistentSubscriptionDroppedByServerException(
						ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
						ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex)
			}) {
			_client = new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(CallInvoker);
			_log = Settings.LoggerFactory?.CreateLogger<EventStorePersistentSubscriptionsClient>()
			       ?? new NullLogger<EventStorePersistentSubscriptionsClient>();
		}
	}
}
