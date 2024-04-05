using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventStore.Client;

/// <summary>
///The client used to manage projections on the EventStoreDB.
/// </summary>
public sealed partial class EventStoreProjectionManagementClient : EventStoreClientBase {
	readonly ILogger _log;

	/// <summary>
	/// Constructs a new <see cref="EventStoreProjectionManagementClient"/>. This method is not intended to be called directly from your code.
	/// </summary>
	/// <param name="options"></param>
	public EventStoreProjectionManagementClient(IOptions<EventStoreClientSettings> options) : this(options.Value) { }

	/// <summary>
	/// Constructs a new <see cref="EventStoreProjectionManagementClient"/>.
	/// </summary>
	/// <param name="settings"></param>
	public EventStoreProjectionManagementClient(EventStoreClientSettings? settings) : base(
		settings,
		new Dictionary<string, Func<RpcException, Exception>>()
	) =>
		_log = settings?.LoggerFactory?.CreateLogger<EventStoreProjectionManagementClient>() ??
		       new NullLogger<EventStoreProjectionManagementClient>();
}