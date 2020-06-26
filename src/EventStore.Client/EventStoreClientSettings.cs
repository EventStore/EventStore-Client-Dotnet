using System;
using System.Collections.Generic;
using System.Net.Http;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClientSettings {
		public IEnumerable<Interceptor>? Interceptors { get; set; }
		public string? ConnectionName { get; set; }
		public Func<HttpMessageHandler>? CreateHttpMessageHandler { get; set; }
		public ILoggerFactory? LoggerFactory { get; set; }
		public ChannelCredentials? ChannelCredentials { get; set; }

		public EventStoreClientOperationOptions OperationOptions { get; set; } =
			EventStoreClientOperationOptions.Default;

		public EventStoreClientConnectivitySettings ConnectivitySettings { get; set; } =
			EventStoreClientConnectivitySettings.Default;

		public UserCredentials? DefaultCredentials { get; set; }
	}
}
