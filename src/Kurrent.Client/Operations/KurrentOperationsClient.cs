using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventStore.Client;

/// <summary>
/// The client used to perform maintenance and other administrative tasks on the KurrentDB.
/// </summary>
public sealed partial class KurrentOperationsClient : KurrentClientBase {
	static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap =
		new() {
			[Constants.Exceptions.ScavengeNotFound] = ex => new ScavengeNotFoundException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.ScavengeId)?.Value
			)
		};

	readonly ILogger _log;

	/// <summary>
	/// Constructs a new <see cref="KurrentOperationsClient"/>. This method is not intended to be called directly in your code.
	/// </summary>
	/// <param name="options"></param>
	public KurrentOperationsClient(IOptions<KurrentClientSettings> options) : this(options.Value) { }

	/// <summary>
	/// Constructs a new <see cref="KurrentOperationsClient"/>.
	/// </summary>
	/// <param name="settings"></param>
	public KurrentOperationsClient(KurrentClientSettings? settings = null) : base(settings, ExceptionMap) =>
		_log = Settings.LoggerFactory?.CreateLogger<KurrentOperationsClient>() ?? new NullLogger<KurrentOperationsClient>();
}
