using System;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventStore.Client {
	/// <summary>
	///The client used to manage projections on the KurrentDB.
	/// </summary>
	public sealed partial class KurrentProjectionManagementClient : KurrentClientBase {
		private readonly ILogger _log;

		/// <summary>
		/// Constructs a new <see cref="KurrentProjectionManagementClient"/>. This method is not intended to be called directly from your code.
		/// </summary>
		/// <param name="options"></param>
		public KurrentProjectionManagementClient(IOptions<KurrentClientSettings> options) : this(options.Value) {
		}

		/// <summary>
		/// Constructs a new <see cref="KurrentProjectionManagementClient"/>.
		/// </summary>
		/// <param name="settings"></param>
		public KurrentProjectionManagementClient(KurrentClientSettings? settings) : base(settings,
			new Dictionary<string, Func<RpcException, Exception>>()) {
			_log = settings?.LoggerFactory?.CreateLogger<KurrentProjectionManagementClient>() ??
			       new NullLogger<KurrentProjectionManagementClient>();
		}
	}
}
