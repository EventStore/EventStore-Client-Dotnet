using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using EventStore.Client.Streams;
#endif
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReadReq = EventStore.Client.Streams.ReadReq;

namespace EventStore.Client {
	/// <summary>
	/// The client used for operations on streams.
	/// </summary>
	public sealed partial class EventStoreClient : EventStoreClientBase {

		private static readonly JsonSerializerOptions StreamMetadataJsonSerializerOptions = new() {
			Converters = {
				StreamMetadataJsonConverter.Instance
			},
		};

		private readonly ILogger<EventStoreClient> _log;
#if NET5_0_OR_GREATER
		private Lazy<StreamAppender> _streamAppenderLazy;
		private StreamAppender _streamAppender => _streamAppenderLazy.Value;
		private readonly CancellationTokenSource _disposedTokenSource;
#endif


		private static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap = new() {
			[Constants.Exceptions.InvalidTransaction] =
				ex => new InvalidTransactionException(ex.Message, ex),
			[Constants.Exceptions.StreamDeleted] = ex => new StreamDeletedException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value ??
				"<unknown>",
				ex),
			[Constants.Exceptions.WrongExpectedVersion] = ex => new WrongExpectedVersionException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!,
				ex.Trailers.GetStreamRevision(Constants.Exceptions.ExpectedVersion),
				ex.Trailers.GetStreamRevision(Constants.Exceptions.ActualVersion),
				ex),
			[Constants.Exceptions.MaximumAppendSizeExceeded] = ex =>
				new MaximumAppendSizeExceededException(
					ex.Trailers.GetIntValueOrDefault(Constants.Exceptions.MaximumAppendSize), ex),
			[Constants.Exceptions.StreamNotFound] = ex => new StreamNotFoundException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!, ex),
			[Constants.Exceptions.MissingRequiredMetadataProperty] = ex => new
				RequiredMetadataPropertyMissingException(
					ex.Trailers.FirstOrDefault(x =>
							x.Key == Constants.Exceptions.MissingRequiredMetadataProperty)
						?.Value!, ex),
		};

		/// <summary>
		/// Constructs a new <see cref="EventStoreClient"/>. This is not intended to be called directly from your code.
		/// </summary>
		/// <param name="options"></param>
		public EventStoreClient(IOptions<EventStoreClientSettings> options) : this(options.Value) {
		}

		/// <summary>
		/// Constructs a new <see cref="EventStoreClient"/>.
		/// </summary>
		/// <param name="settings"></param>
		public EventStoreClient(EventStoreClientSettings? settings = null) : base(settings, ExceptionMap) {
			_log = Settings.LoggerFactory?.CreateLogger<EventStoreClient>() ?? new NullLogger<EventStoreClient>();
#if NET5_0_OR_GREATER
			_disposedTokenSource = new CancellationTokenSource();
			_streamAppenderLazy = new Lazy<StreamAppender>(CreateStreamAppender);
#endif
		}

#if NET5_0_OR_GREATER
		private void SwapStreamAppender(Exception ex) =>
			Interlocked.Exchange(ref _streamAppenderLazy,
					new Lazy<StreamAppender>(CreateStreamAppender))
				.Value.Dispose();

		// todo: might be nice to have two different kinds of appenders and we decide which to instantiate according to the server caps.
		private StreamAppender CreateStreamAppender() {
			return new StreamAppender(Settings, GetCall(), _disposedTokenSource.Token, SwapStreamAppender);

			async Task<AsyncDuplexStreamingCall<BatchAppendReq, BatchAppendResp>?> GetCall() {
				var channelInfo = await GetChannelInfo(_disposedTokenSource.Token).ConfigureAwait(false);
				if (!channelInfo.ServerCapabilities.SupportsBatchAppend)
					return null;

				var client = new Streams.Streams.StreamsClient(channelInfo.CallInvoker);

				return client.BatchAppend(EventStoreCallOptions.CreateStreaming(Settings,
					userCredentials: Settings.DefaultCredentials, cancellationToken: _disposedTokenSource.Token));
			}
		}
#endif

		private static ReadReq.Types.Options.Types.FilterOptions? GetFilterOptions(
			SubscriptionFilterOptions? filterOptions) {
			if (filterOptions == null) {
				return null;
			}

			var filter = filterOptions.Filter;

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

			if (options == null) {
				return null;
			}

			if (filter.MaxSearchWindow.HasValue) {
				options.Max = filter.MaxSearchWindow.Value;
			} else {
				options.Count = new Empty();
			}

			options.CheckpointIntervalMultiplier = filterOptions.CheckpointInterval;

			return options;
		}

#if !GRPC_CORE && NET5_0_OR_GREATER
		/// <inheritdoc />
		public override void Dispose() {
			if (_streamAppenderLazy.IsValueCreated)
				_streamAppenderLazy.Value.Dispose();
			_disposedTokenSource.Dispose();
			base.Dispose();
		}
#endif

#if NET5_0_OR_GREATER
		/// <inheritdoc />
		public override async ValueTask DisposeAsync() {
			if (_streamAppenderLazy.IsValueCreated)
				_streamAppenderLazy.Value.Dispose();
			_disposedTokenSource.Dispose();
			await base.DisposeAsync().ConfigureAwait(false);
		}
#endif
	}
}
