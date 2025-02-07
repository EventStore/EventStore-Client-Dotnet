using System.Text.Json;
using System.Threading.Channels;
using EventStore.Client.Serialization;
using Grpc.Core;
using Kurrent.Client.Core.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReadReq = EventStore.Client.Streams.ReadReq;

namespace EventStore.Client {
	/// <summary>
	/// The client used for operations on streams.
	/// </summary>
	public sealed partial class KurrentClient : KurrentClientBase {
		static readonly JsonSerializerOptions StreamMetadataJsonSerializerOptions = new() {
			Converters = {
				StreamMetadataJsonConverter.Instance
			},
		};

		static BoundedChannelOptions ReadBoundedChannelOptions = new(1) {
			SingleReader                  = true,
			SingleWriter                  = true,
			AllowSynchronousContinuations = true
		};

		readonly ILogger<KurrentClient>  _log;
		Lazy<StreamAppender>             _batchAppenderLazy;
		StreamAppender                   BatchAppender => _batchAppenderLazy.Value;
		readonly CancellationTokenSource _disposedTokenSource;

		static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap = new() {
			[Constants.Exceptions.InvalidTransaction] = ex => new InvalidTransactionException(ex.Message, ex),
			[Constants.Exceptions.StreamDeleted] = ex => new StreamDeletedException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value ?? "<unknown>",
				ex
			),
			[Constants.Exceptions.WrongExpectedVersion] = ex => new WrongExpectedVersionException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!,
				ex.Trailers.GetStreamRevision(Constants.Exceptions.ExpectedVersion),
				ex.Trailers.GetStreamRevision(Constants.Exceptions.ActualVersion),
				ex,
				ex.Message
			),
			[Constants.Exceptions.MaximumAppendSizeExceeded] = ex => new MaximumAppendSizeExceededException(
				ex.Trailers.GetIntValueOrDefault(Constants.Exceptions.MaximumAppendSize),
				ex
			),
			[Constants.Exceptions.StreamNotFound] = ex => new StreamNotFoundException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!,
				ex
			),
			[Constants.Exceptions.MissingRequiredMetadataProperty] = ex => new RequiredMetadataPropertyMissingException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.MissingRequiredMetadataProperty)?.Value!,
				ex
			),
		};

		/// <summary>
		/// Constructs a new <see cref="KurrentClient"/>. This is not intended to be called directly from your code.
		/// </summary>
		/// <param name="options"></param>
		public KurrentClient(IOptions<KurrentClientSettings> options) : this(options.Value) { }

		/// <summary>
		/// Constructs a new <see cref="KurrentClient"/>.
		/// </summary>
		/// <param name="settings"></param>
		public KurrentClient(KurrentClientSettings? settings = null) : base(settings, ExceptionMap) {
			_log = Settings.LoggerFactory?.CreateLogger<KurrentClient>() ?? new NullLogger<KurrentClient>();
			_disposedTokenSource = new CancellationTokenSource();
			_batchAppenderLazy = new Lazy<StreamAppender>(CreateStreamAppender);
			
			_messageSerializer = MessageSerializer.From(settings?.Serialization);
		}

		void SwapStreamAppender(Exception ex) =>
			Interlocked.Exchange(ref _batchAppenderLazy, new Lazy<StreamAppender>(CreateStreamAppender)).Value
				.Dispose();

		// todo: might be nice to have two different kinds of appenders and we decide which to instantiate according to the server caps.
		StreamAppender CreateStreamAppender() => new StreamAppender(
			Settings,
			GetChannelInfo(_disposedTokenSource.Token),
			_disposedTokenSource.Token,
			SwapStreamAppender
		);

		static ReadReq.Types.Options.Types.FilterOptions? GetFilterOptions(
			IEventFilter? filter, uint checkpointInterval = 0
		) {
			if (filter == null
			 || filter.Equals(StreamFilter.None)
			 || filter.Equals(EventTypeFilter.None))
				return null;

			var options = filter switch {
				StreamFilter => new ReadReq.Types.Options.Types.FilterOptions {
					StreamIdentifier = (filter.Prefixes, filter.Regex) switch {
						(_, _)
							when (filter.Prefixes?.Length ?? 0) == 0 &&
							     filter.Regex != RegularFilterExpression.None =>
							new ReadReq.Types.Options.Types.FilterOptions.Types.Expression
								{ Regex = filter.Regex },
						(_, _)
							when (filter.Prefixes?.Length ?? 0) != 0 &&
							     filter.Regex == RegularFilterExpression.None =>
							new ReadReq.Types.Options.Types.FilterOptions.Types.Expression {
								Prefix = { Array.ConvertAll(filter.Prefixes!, e => e.ToString()) }
							},
						_ => throw new InvalidOperationException()
					}
				},
				EventTypeFilter => new ReadReq.Types.Options.Types.FilterOptions {
					EventType = (filter.Prefixes, filter.Regex) switch {
						(_, _)
							when (filter.Prefixes?.Length ?? 0) == 0 &&
							     filter.Regex != RegularFilterExpression.None =>
							new ReadReq.Types.Options.Types.FilterOptions.Types.Expression
								{ Regex = filter.Regex },
						(_, _)
							when (filter.Prefixes?.Length ?? 0) != 0 &&
							     filter.Regex == RegularFilterExpression.None =>
							new ReadReq.Types.Options.Types.FilterOptions.Types.Expression {
								Prefix = { Array.ConvertAll(filter.Prefixes!, e => e.ToString()) }
							},
						_ => throw new InvalidOperationException()
					}
				},
				_ => null
			};

			if (options == null)
				return null;

			if (filter.MaxSearchWindow.HasValue)
				options.Max = filter.MaxSearchWindow.Value;
			else
				options.Count = new Empty();

			options.CheckpointIntervalMultiplier = checkpointInterval;

			return options;
		}

		static ReadReq.Types.Options.Types.FilterOptions? GetFilterOptions(
			SubscriptionFilterOptions? filterOptions
		)
			=> filterOptions == null ? null : GetFilterOptions(filterOptions.Filter, filterOptions.CheckpointInterval);

		/// <inheritdoc />
		public override void Dispose() {
			if (_batchAppenderLazy.IsValueCreated)
				_batchAppenderLazy.Value.Dispose();

			_disposedTokenSource.Dispose();
			base.Dispose();
		}

		/// <inheritdoc />
		public override async ValueTask DisposeAsync() {
			if (_batchAppenderLazy.IsValueCreated)
				_batchAppenderLazy.Value.Dispose();

			_disposedTokenSource.Dispose();
			await base.DisposeAsync().ConfigureAwait(false);
		}
	}
}
