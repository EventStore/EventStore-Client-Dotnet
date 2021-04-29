using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReadReq = EventStore.Client.Streams.ReadReq;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The client used for operations on streams.
	/// </summary>
	public partial class EventStoreClient : EventStoreClientBase, IDisposable, IAsyncDisposable
	{
		private static readonly JsonSerializerOptions StreamMetadataJsonSerializerOptions = new JsonSerializerOptions {
			Converters = {
				StreamMetadataJsonConverter.Instance
			},
		};

		private readonly ILogger<EventStoreClient> _log;
		private StreamAppender _streamAppender;
		private readonly CancellationTokenSource _disposedTokenSource;

		private static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap =
			new Dictionary<string, Func<RpcException, Exception>> {
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
			_disposedTokenSource = new CancellationTokenSource();
			_streamAppender = CreateStreamAppender();
		}

		private void SwapStreamAppender(Exception ex) =>
			Interlocked.Exchange(ref _streamAppender, CreateStreamAppender());

		private StreamAppender CreateStreamAppender() {
			return new StreamAppender(Settings, GetCall(), _disposedTokenSource.Token, SwapStreamAppender);

			async Task<AsyncDuplexStreamingCall<BatchAppendReq, BatchAppendResp>> GetCall() {
				var callInvoker = await SelectCallInvoker(_disposedTokenSource.Token).ConfigureAwait(false);
				var client = new Streams.Streams.StreamsClient(callInvoker);
				var operationOptions = Settings.OperationOptions.Clone();
				operationOptions.TimeoutAfter = new TimeSpan?();

				return client.BatchAppend(EventStoreCallOptions.Create(Settings,
					operationOptions, Settings.DefaultCredentials, _disposedTokenSource.Token));
			}
		}

		private static FilterOptions? GetFilterOptions(
			SubscriptionFilterOptions? filterOptions) {
			if (filterOptions == null) {
				return null;
			}

			var filter = filterOptions.Filter;

			var options = filter switch {
				StreamFilter _ => new FilterOptions {
					StreamName = (filter.Prefixes, filter.Regex) switch {
						(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) == 0 &&
						     filter.Regex != RegularFilterExpression.None =>
						new FilterOptions.Types.Expression
							{Regex = filter.Regex},
						(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) != 0 &&
						     filter.Regex == RegularFilterExpression.None =>
						new FilterOptions.Types.Expression {
							Prefix = {Array.ConvertAll(filter.Prefixes!, e => e.ToString())}
						},
						_ => throw new InvalidOperationException()
					}
				},
				EventTypeFilter _ => new FilterOptions {
					EventType = (filter.Prefixes, filter.Regex) switch {
						(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) == 0 &&
						     filter.Regex != RegularFilterExpression.None =>
						new FilterOptions.Types.Expression
							{Regex = filter.Regex},
						(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) != 0 &&
						     filter.Regex == RegularFilterExpression.None =>
						new FilterOptions.Types.Expression {
							Prefix = {Array.ConvertAll(filter.Prefixes!, e => e.ToString())}
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

		void IDisposable.Dispose() {
			_streamAppender.Dispose();
			_disposedTokenSource.Dispose();
			base.Dispose();
		}

		async ValueTask IAsyncDisposable.DisposeAsync() {
			_streamAppender.Dispose();
			_disposedTokenSource.Dispose();
			await base.DisposeAsync().ConfigureAwait(false);
		}
	}
}
