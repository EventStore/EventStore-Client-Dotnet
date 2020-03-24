using System;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient : EventStoreClientBase {
		private readonly Projections.Projections.ProjectionsClient _client;
		private readonly ILogger _log;

		public EventStoreProjectionManagementClient(IOptions<EventStoreClientSettings> options) : this(options.Value) {
		}

		public EventStoreProjectionManagementClient(EventStoreClientSettings? settings) : base(settings,
			new Dictionary<string, Func<RpcException, Exception>>()) {
			_client = new Projections.Projections.ProjectionsClient(CallInvoker);
			_log = settings?.LoggerFactory?.CreateLogger<EventStoreProjectionManagementClient>() ??
			       new NullLogger<EventStoreProjectionManagementClient>();
		}
	}
}
