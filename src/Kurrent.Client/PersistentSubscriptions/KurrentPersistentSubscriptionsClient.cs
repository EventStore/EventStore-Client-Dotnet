using System.Text.Encodings.Web;
using System.Threading.Channels;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventStore.Client {
	/// <summary>
	/// The client used to manage persistent subscriptions in the KurrentDB.
	/// </summary>
	public sealed partial class KurrentPersistentSubscriptionsClient : KurrentClientBase {
		private static BoundedChannelOptions ReadBoundedChannelOptions = new (1) {
			SingleReader = true,
			SingleWriter = true,
			AllowSynchronousContinuations = true
		};

		private readonly ILogger _log;

		/// <summary>
		/// Constructs a new <see cref="KurrentPersistentSubscriptionsClient"/>.
		/// </summary>
		public KurrentPersistentSubscriptionsClient(KurrentClientSettings? settings) : base(settings,
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
			_log = Settings.LoggerFactory?.CreateLogger<KurrentPersistentSubscriptionsClient>()
			       ?? new NullLogger<KurrentPersistentSubscriptionsClient>();
		}
		
		private static string UrlEncode(string s) {
			return UrlEncoder.Default.Encode(s);
		}
	}
}
