using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
		private async IAsyncEnumerable<ResolvedEvent> ReadAllAsync(
			Direction direction,
			Position position,
			long maxCount,
			EventStoreClientOperationOptions operationOptions,
			bool resolveLinkTos = false,
			UserCredentials? userCredentials = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) {
			await foreach (var (confirmation, _, resolvedEvent) in ReadInternal(new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = direction switch {
							Direction.Backwards => ReadReq.Types.Options.Types.ReadDirection.Backwards,
							Direction.Forwards => ReadReq.Types.Options.Types.ReadDirection.Forwards,
							_ => throw InvalidOption(direction)
						},
						ResolveLinks = resolveLinkTos,
						All = ReadReq.Types.Options.Types.AllOptions.FromPosition(position),
						Count = (ulong)maxCount,
					}
				},
				operationOptions, userCredentials, cancellationToken)) {
				if (confirmation != SubscriptionConfirmation.None) {
					continue;
				}

				yield return resolvedEvent;
			}
		}

		/// <summary>
		/// Asynchronously reads all events.
		/// </summary>
		/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
		/// <param name="position">The <see cref="Position"/> to start reading from.</param>
		/// <param name="maxCount">The maximum count to read.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public IAsyncEnumerable<ResolvedEvent> ReadAllAsync(
			Direction direction,
			Position position,
			long maxCount = long.MaxValue,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			bool resolveLinkTos = false,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return ReadAllAsync(direction, position, maxCount, operationOptions, resolveLinkTos, userCredentials,
				cancellationToken);
		}

		private ReadStreamResult ReadStreamAsync(Direction direction,
			string streamName, StreamPosition revision, long maxCount,
			EventStoreClientOperationOptions operationOptions, bool resolveLinkTos = false,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			return new ReadStreamResult(SelectCallInvoker, new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = direction switch {
							Direction.Backwards => ReadReq.Types.Options.Types.ReadDirection.Backwards,
							Direction.Forwards => ReadReq.Types.Options.Types.ReadDirection.Forwards,
							_ => throw InvalidOption(direction)
						},
						ResolveLinks = resolveLinkTos,
						Stream = ReadReq.Types.Options.Types.StreamOptions.FromStreamNameAndRevision(streamName,
							revision),
						Count = (ulong)maxCount
					}
				}, Settings, operationOptions, userCredentials, cancellationToken);
		}

		/// <summary>
		/// Asynchronously reads all the events from a stream.
		///
		/// The result could also be inspected as a means to avoid handling exceptions as the <see cref="ReadState"/> would indicate whether or not the stream is readable./>
		/// </summary>
		/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
		/// <param name="streamName">The name of the stream to read.</param>
		/// <param name="revision">The <see cref="StreamRevision"/> to start reading from.</param>
		/// <param name="maxCount">The number of events to read from the stream.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public ReadStreamResult ReadStreamAsync(
			Direction direction,
			string streamName,
			StreamPosition revision,
			long maxCount = long.MaxValue,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			bool resolveLinkTos = false,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return ReadStreamAsync(direction, streamName, revision, maxCount, operationOptions, resolveLinkTos, userCredentials, cancellationToken);
		}

		/// <summary>
		///  Represents a fold result
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public struct FoldResult<T> {
			/// <summary>
			/// the position of the last event folded
			/// </summary>
			public StreamRevision Revision { get; }
			/// <summary>
			/// the fold value
			/// </summary>
			public T Value { get; }

			/// <summary>
			/// build a fold resuult
			/// </summary>
			/// <param name="revision"></param>
			/// <param name="value"></param>
			public FoldResult(StreamRevision revision, T value) {
				Revision = revision;
				Value = value;
			}
		}

		/// <summary>
		/// folds a stream using provided aggregator and seed
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="E"></typeparam>
		/// <param name="deserialize"></param>
		/// <param name="aggregator"></param>
		/// <param name="streamName"></param>
		/// <param name="revision"></param>
		/// <param name="seed"></param>
		/// <param name="configureOperationOptions"></param>
		/// <param name="resolveLinkTos"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async ValueTask<FoldResult<T>> FoldStreamAsync<T,E>(
			Func<ResolvedEvent, IEnumerable<E>> deserialize,
			Func<T,E,T> aggregator,
			string streamName,
			StreamPosition revision,
			T seed,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			bool resolveLinkTos = false,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			var readReq =
				new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
						ResolveLinks = resolveLinkTos,
						Stream = ReadReq.Types.Options.Types.StreamOptions.FromStreamNameAndRevision(streamName, revision),
						Count = long.MaxValue
					}
					
				};
			if (readReq.Options.Filter == null) {
				readReq.Options.NoFilter = new Empty();
			}

			readReq.Options.UuidOption = new ReadReq.Types.Options.Types.UUIDOption { Structured = new Empty() };

			var call = 
					_client.Read(readReq,
						EventStoreCallOptions.Create(Settings, operationOptions, userCredentials, cancellationToken))
					.ResponseStream.ReadAllAsync().GetAsyncEnumerator();

			if (!await call.MoveNextAsync(cancellationToken).ConfigureAwait(false) || call.Current.ContentCase == ReadResp.ContentOneofCase.StreamNotFound) {
				return new FoldResult<T>(StreamRevision.None, seed);
			}

			while (true) {
				if (call.Current.ContentCase == ReadResp.ContentOneofCase.Event) {
					var re = ConvertToResolvedEvent(call.Current.Event);
					foreach (var e in deserialize(re))
						seed = aggregator(seed, e);
					if (!await call.MoveNextAsync(cancellationToken).ConfigureAwait(false))
						return new FoldResult<T>(StreamRevision.FromStreamPosition(re.Event.EventNumber), seed);

				}
			}
		}

		/// <summary>
		/// A class that represents the result of a read operation.
		/// </summary>
		public class ReadStreamResult : IAsyncEnumerable<ResolvedEvent>, IAsyncEnumerator<ResolvedEvent> {
			private readonly string _streamName;
			private readonly TaskCompletionSource<IAsyncEnumerator<ReadResp>> _callSource;

			private bool _moved;
			private CancellationToken _cancellationToken;

			internal ReadStreamResult(
				Func<CancellationToken, Task<CallInvoker>> selectCallInvoker,
				ReadReq request,
				EventStoreClientSettings settings,
				EventStoreClientOperationOptions operationOptions,
				UserCredentials? userCredentials, CancellationToken cancellationToken) {
				if (request.Options.CountOptionCase == ReadReq.Types.Options.CountOptionOneofCase.Count &&
				    request.Options.Count <= 0) {
					throw new ArgumentOutOfRangeException("count");
				}

				_streamName = request.Options.Stream.StreamIdentifier;

				if (request.Options.Filter == null) {
					request.Options.NoFilter = new Empty();
				}

				request.Options.UuidOption = new ReadReq.Types.Options.Types.UUIDOption {Structured = new Empty()};
				_callSource = new TaskCompletionSource<IAsyncEnumerator<ReadResp>>();
				_ = Task.Run(async () => {
					var client = new Streams.Streams.StreamsClient(await selectCallInvoker(cancellationToken)
						.ConfigureAwait(false));
					_callSource.SetResult(client
						.Read(request,
							EventStoreCallOptions.Create(settings, operationOptions, userCredentials,
								cancellationToken)).ResponseStream.ReadAllAsync().GetAsyncEnumerator());
				});
				_moved = false;

				ReadState = GetStateInternal();

				async Task<ReadState> GetStateInternal() {
					var call = await _callSource.Task.ConfigureAwait(false);

					_moved = await call.MoveNextAsync(cancellationToken).ConfigureAwait(false);
					return call.Current?.ContentCase switch {
						ReadResp.ContentOneofCase.StreamNotFound => Client.ReadState.StreamNotFound,
						_ => Client.ReadState.Ok
					};
				}

				Current = default;
			}

			/// <summary>
			/// The <see cref="ReadState"/>.
			/// </summary>
			public Task<ReadState> ReadState { get; }

			/// <inheritdoc />
			public IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(
				CancellationToken cancellationToken = new CancellationToken()) {
				_cancellationToken = cancellationToken;
				return this;
			}

			/// <inheritdoc />
			public async ValueTask DisposeAsync() =>
				await (await _callSource.Task.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);

			/// <inheritdoc />
			public async ValueTask<bool> MoveNextAsync() {
				var state = await ReadState.ConfigureAwait(false);
				if (state != Client.ReadState.Ok) {
					throw ExceptionFromState(state, _streamName);
				}

				var call = await _callSource.Task.ConfigureAwait(false);

				if (_moved) {
					_moved = false;
					if (IsCurrentItemEvent()) {
						return true;
					}
				}

				while (await call.MoveNextAsync(_cancellationToken).ConfigureAwait(false)) {
					if (IsCurrentItemEvent()) {
						return true;
					}
				}

				Current = default;
				return false;

				bool IsCurrentItemEvent() {
					var item = ConvertToItem(call.Current);
					if (!item.HasValue) {
						return false;
					}
					var (confirmation, position, @event) = item.Value;
					if (confirmation == SubscriptionConfirmation.None && position == null) {
						Current = @event;
						return true;
					}

					return false;
				}
			}

			private static Exception ExceptionFromState(ReadState state, string streamName) =>
				state switch {
					Client.ReadState.StreamNotFound => new StreamNotFoundException(streamName),
					_ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
				};

			/// <inheritdoc />
			public ResolvedEvent Current { get; private set; }
		}


		private async IAsyncEnumerable<(SubscriptionConfirmation, Position?, ResolvedEvent)> ReadInternal(
			ReadReq request,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials,
			[EnumeratorCancellation] CancellationToken cancellationToken) {
			if (request.Options.CountOptionCase == ReadReq.Types.Options.CountOptionOneofCase.Count &&
			    request.Options.Count <= 0) {
				throw new ArgumentOutOfRangeException("count");
			}

			if (request.Options.Filter == null) {
				request.Options.NoFilter = new Empty();
			}

			request.Options.UuidOption = new ReadReq.Types.Options.Types.UUIDOption {Structured = new Empty()};

			using var call = new Streams.Streams.StreamsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).Read(request,
				EventStoreCallOptions.Create(Settings, operationOptions, userCredentials, cancellationToken));

			await foreach (var e in call.ResponseStream
				.ReadAllAsync(cancellationToken)
				.Select(ConvertToItem)
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false)) {
				if (e.HasValue) {
					yield return e.Value;
				}
			}
		}

		private static (SubscriptionConfirmation, Position?, ResolvedEvent)? ConvertToItem(ReadResp response) =>
			response.ContentCase switch {
				ReadResp.ContentOneofCase.Confirmation => (
					new SubscriptionConfirmation(response.Confirmation.SubscriptionId), null, default),
				ReadResp.ContentOneofCase.Event => (SubscriptionConfirmation.None,
					null,
					ConvertToResolvedEvent(response.Event)),
				ReadResp.ContentOneofCase.Checkpoint => (SubscriptionConfirmation.None,
					new Position(response.Checkpoint.CommitPosition, response.Checkpoint.PreparePosition),
					default),
				_ => null
			};

		private static ResolvedEvent ConvertToResolvedEvent(ReadResp.Types.ReadEvent readEvent) =>
			new ResolvedEvent(
				ConvertToEventRecord(readEvent.Event)!,
				ConvertToEventRecord(readEvent.Link),
				readEvent.PositionCase switch {
					ReadResp.Types.ReadEvent.PositionOneofCase.CommitPosition => readEvent.CommitPosition,
					_ => null
				});

		private static EventRecord? ConvertToEventRecord(ReadResp.Types.ReadEvent.Types.RecordedEvent? e) =>
			e == null
				? null
				: new EventRecord(
					e.StreamIdentifier,
					Uuid.FromDto(e.Id),
					new StreamPosition(e.StreamRevision),
					new Position(e.CommitPosition, e.PreparePosition),
					e.Metadata,
					e.Data.ToByteArray(),
					e.CustomMetadata.ToByteArray());
	}
}
