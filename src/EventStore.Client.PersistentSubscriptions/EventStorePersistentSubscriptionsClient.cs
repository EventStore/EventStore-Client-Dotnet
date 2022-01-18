using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventStore.Client {
	/// <summary>
	/// The client used to manage persistent subscriptions in the EventStoreDB.
	/// </summary>
	public sealed partial class EventStorePersistentSubscriptionsClient : EventStoreClientBase {
		private readonly ILogger _log;

		/// <summary>
		/// Constructs a new <see cref="EventStorePersistentSubscriptionsClient"/>.
		/// </summary>
		public EventStorePersistentSubscriptionsClient(EventStoreClientSettings? settings) : base(settings,
			new Dictionary<string, Func<RpcException, Exception>> {
				[Constants.Exceptions.PersistentSubscriptionDoesNotExist] = ex => new
					PersistentSubscriptionNotFoundException(
						ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
						ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.GroupName)?.Value ?? "", ex),
				[Constants.Exceptions.MaximumSubscribersReached] = ex => new
					MaximumSubscribersReachedException(
						ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
						ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex),
				[Constants.Exceptions.PersistentSubscriptionDropped] = ex => new
					PersistentSubscriptionDroppedByServerException(
						ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
						ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex)
			}) {
			_log = Settings.LoggerFactory?.CreateLogger<EventStorePersistentSubscriptionsClient>()
			       ?? new NullLogger<EventStorePersistentSubscriptionsClient>();
		}
		
		private static string UrlEncode(string s) {
			return UrlEncoder.Default.Encode(s);
		}
	}
}
