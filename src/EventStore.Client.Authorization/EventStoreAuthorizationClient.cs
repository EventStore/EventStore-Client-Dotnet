using System;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventStore.Client {
	/// <summary>
	/// The client used to query Authorization APIs on the EventStoreDB.
	/// </summary>
	public sealed partial class EventStoreAuthorizationClient : EventStoreClientBase {
		private static readonly IDictionary<string, Func<RpcException, Exception>> ExceptionMap =
			new Dictionary<string, Func<RpcException, Exception>>();

		private readonly ILogger _log;

		/// <summary>
		/// Constructs a new <see cref="EventStoreAuthorizationClient"/>. This method is not intended to be called directly in your code.
		/// </summary>
		/// <param name="options"></param>
		public EventStoreAuthorizationClient(IOptions<EventStoreClientSettings> options) : this(options.Value) {
		}

		/// <summary>
		/// Constructs a new <see cref="EventStoreAuthorizationClient"/>.
		/// </summary>
		/// <param name="settings"></param>
		public EventStoreAuthorizationClient(EventStoreClientSettings? settings = null) : base(settings, ExceptionMap) {
			_log = Settings.LoggerFactory?.CreateLogger<EventStoreAuthorizationClient>() ??
			       new NullLogger<EventStoreAuthorizationClient>();
		}
	}
}
