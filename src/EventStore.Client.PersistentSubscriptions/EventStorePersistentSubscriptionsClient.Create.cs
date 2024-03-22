using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;

namespace EventStore.Client {
	partial class EventStorePersistentSubscriptionsClient {
		private static readonly IDictionary<string, CreateReq.Types.ConsumerStrategy> NamedConsumerStrategyToCreateProto
			= new Dictionary<string, CreateReq.Types.ConsumerStrategy> {
				[SystemConsumerStrategies.DispatchToSingle] = CreateReq.Types.ConsumerStrategy.DispatchToSingle,
				[SystemConsumerStrategies.RoundRobin] = CreateReq.Types.ConsumerStrategy.RoundRobin,
				[SystemConsumerStrategies.Pinned] = CreateReq.Types.ConsumerStrategy.Pinned,
			};
			
		private static CreateReq.Types.StreamOptions StreamOptionsForCreateProto(string streamName, StreamPosition position) {
			if (position == StreamPosition.Start) {
				return new CreateReq.Types.StreamOptions {
					StreamIdentifier = streamName,
					Start = new Empty()
				};
			}

			if (position == StreamPosition.End) {
				return new CreateReq.Types.StreamOptions {
					StreamIdentifier = streamName,
					End = new Empty()
				};
			}

			return new CreateReq.Types.StreamOptions {
				StreamIdentifier = streamName,
				Revision = position.ToUInt64()
			};
		}

		private static CreateReq.Types.AllOptions AllOptionsForCreateProto(Position position, IEventFilter? filter) {
			var allFilter = GetFilterOptions(filter);
			CreateReq.Types.AllOptions allOptions;
			if (position == Position.Start) {
				allOptions = new CreateReq.Types.AllOptions {
					Start = new Empty(),
				};
			} else if (position == Position.End) {
				allOptions = new CreateReq.Types.AllOptions {
					End = new Empty()
				};
			} else {
				allOptions = new CreateReq.Types.AllOptions {
					Position = new CreateReq.Types.Position {
						CommitPosition = position.CommitPosition,
						PreparePosition = position.PreparePosition
					}
				};
			}

			if (allFilter is null) {
				allOptions.NoFilter = new Empty();
			} else {
				allOptions.Filter = allFilter;
			}

			return allOptions;
		}

		private static CreateReq.Types.AllOptions.Types.FilterOptions? GetFilterOptions(IEventFilter? filter) {
			if (filter == null) {
				return null;
			}

			var options = filter switch {
				StreamFilter _ => new CreateReq.Types.AllOptions.Types.FilterOptions {
					StreamIdentifier = (filter.Prefixes, filter.Regex) switch {
						(PrefixFilterExpression[] _, RegularFilterExpression _)
							when (filter.Prefixes?.Length ?? 0) == 0 &&
							     filter.Regex != RegularFilterExpression.None =>
							new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression
								{Regex = filter.Regex},
						(PrefixFilterExpression[] _, RegularFilterExpression _)
							when (filter.Prefixes?.Length ?? 0) != 0 &&
							     filter.Regex == RegularFilterExpression.None =>
							new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression {
								Prefix = {Array.ConvertAll(filter.Prefixes!, e => e.ToString())}
							},
						_ => throw new InvalidOperationException()
					}
				},
				EventTypeFilter _ => new CreateReq.Types.AllOptions.Types.FilterOptions {
					EventType = (filter.Prefixes, filter.Regex) switch {
						(PrefixFilterExpression[] _, RegularFilterExpression _)
							when (filter.Prefixes?.Length ?? 0) == 0 &&
							     filter.Regex != RegularFilterExpression.None =>
							new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression
								{Regex = filter.Regex},
						(PrefixFilterExpression[] _, RegularFilterExpression _)
							when (filter.Prefixes?.Length ?? 0) != 0 &&
							     filter.Regex == RegularFilterExpression.None =>
							new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression {
								Prefix = {Array.ConvertAll(filter.Prefixes!, e => e.ToString())}
							},
						_ => throw new InvalidOperationException()
					}
				},
				_ => throw new InvalidOperationException()
			};

			if (filter.MaxSearchWindow.HasValue) {
				options.Max = filter.MaxSearchWindow.Value;
			} else {
				options.Count = new Empty();
			}

			return options;
		}


		/// <summary>
		/// Creates a persistent subscription.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task CreateToStreamAsync(
			string streamName, string groupName, PersistentSubscriptionSettings settings,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null, UserCertificate? userCertificate = null,
			CancellationToken cancellationToken = default
		) =>
			await CreateInternalAsync(
					streamName,
					groupName,
					null,
					settings,
					deadline,
					userCredentials,
					userCertificate,
					cancellationToken
				)
				.ConfigureAwait(false);
		
		/// <summary>
		/// Creates a persistent subscription.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		[Obsolete("CreateAsync is no longer supported. Use CreateToStreamAsync instead.", false)]
		public async Task CreateAsync(
			string streamName, string groupName, PersistentSubscriptionSettings settings,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null, UserCertificate? userCertificate = null,
			CancellationToken cancellationToken = default
		) =>
			await CreateInternalAsync(
					streamName,
					groupName,
					null,
					settings,
					deadline,
					userCredentials,
					userCertificate,
					cancellationToken
				)
				.ConfigureAwait(false);
		
		/// <summary>
		/// Creates a filtered persistent subscription to $all.
		/// </summary>
		public async Task CreateToAllAsync(
			string groupName, IEventFilter eventFilter,
			PersistentSubscriptionSettings settings, TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			UserCertificate? userCertificate = null,
			CancellationToken cancellationToken = default
		) =>
			await CreateInternalAsync(
					SystemStreams.AllStream,
					groupName,
					eventFilter,
					settings,
					deadline,
					userCredentials,
					userCertificate,
					cancellationToken
				)
				.ConfigureAwait(false);

		/// <summary>
		/// Creates a persistent subscription to $all.
		/// </summary>
		public async Task CreateToAllAsync(
			string groupName, PersistentSubscriptionSettings settings,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null, UserCertificate? userCertificate = null,
			CancellationToken cancellationToken = default
		) =>
			await CreateInternalAsync(
					SystemStreams.AllStream,
					groupName,
					null,
					settings,
					deadline,
					userCredentials,
					userCertificate,
					cancellationToken
				)
				.ConfigureAwait(false);

		private async Task CreateInternalAsync(
			string streamName, string groupName, IEventFilter? eventFilter,
			PersistentSubscriptionSettings settings, TimeSpan? deadline, UserCredentials? userCredentials,
			UserCertificate? userCertificate,
			CancellationToken cancellationToken
		) {
			if (streamName is null) {
				throw new ArgumentNullException(nameof(streamName));
			}

			if (groupName is null) {
				throw new ArgumentNullException(nameof(groupName));
			}

			if (settings is null) {
				throw new ArgumentNullException(nameof(settings));
			}

			if (settings.ConsumerStrategyName is null) {
				throw new ArgumentNullException(nameof(settings.ConsumerStrategyName));
			}

			if (streamName != SystemStreams.AllStream && settings.StartFrom != null &&
			    settings.StartFrom is not StreamPosition) {
				throw new ArgumentException(
					$"{nameof(settings.StartFrom)} must be of type '{nameof(StreamPosition)}' when subscribing to a stream");
			}

			if (streamName == SystemStreams.AllStream && settings.StartFrom != null &&
			    settings.StartFrom is not Position) {
				throw new ArgumentException(
					$"{nameof(settings.StartFrom)} must be of type '{nameof(Position)}' when subscribing to {SystemStreams.AllStream}");
			}

			if (eventFilter != null && streamName != SystemStreams.AllStream) {
				throw new ArgumentException(
					$"Filters are only supported when subscribing to {SystemStreams.AllStream}");
			}

			if (!NamedConsumerStrategyToCreateProto.ContainsKey(settings.ConsumerStrategyName)) {
				throw new ArgumentException(
					"The specified consumer strategy is not supported, specify one of the SystemConsumerStrategies");
			}

			var channelInfo = await GetChannelInfo(userCertificate?.Certificate, cancellationToken).ConfigureAwait(false);

			if (streamName == SystemStreams.AllStream &&
			    !channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll) {
				throw new InvalidOperationException("The server does not support persistent subscriptions to $all.");
			}

			using var call = new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Stream = streamName != SystemStreams.AllStream
						? StreamOptionsForCreateProto(streamName,
							(StreamPosition)(settings.StartFrom ?? StreamPosition.End))
						: null,
					All = streamName == SystemStreams.AllStream
						? AllOptionsForCreateProto((Position)(settings.StartFrom ?? Position.End), eventFilter)
						: null,
#pragma warning disable 612
					StreamIdentifier =
						streamName != SystemStreams.AllStream
							? streamName
							: string.Empty, /*for backwards compatibility*/
#pragma warning restore 612
					GroupName = groupName,
					Settings = new CreateReq.Types.Settings {
#pragma warning disable 612
						Revision = streamName != SystemStreams.AllStream
							? ((StreamPosition)(settings.StartFrom ?? StreamPosition.End)).ToUInt64()
							: default, /*for backwards compatibility*/
#pragma warning restore 612
						CheckpointAfterMs = (int)settings.CheckPointAfter.TotalMilliseconds,
						ExtraStatistics = settings.ExtraStatistics,
						MessageTimeoutMs = (int)settings.MessageTimeout.TotalMilliseconds,
						ResolveLinks = settings.ResolveLinkTos,
						HistoryBufferSize = settings.HistoryBufferSize,
						LiveBufferSize = settings.LiveBufferSize,
						MaxCheckpointCount = settings.CheckPointUpperBound,
						MaxRetryCount = settings.MaxRetryCount,
						MaxSubscriberCount = settings.MaxSubscriberCount,
						MinCheckpointCount = settings.CheckPointLowerBound,
#pragma warning disable 612
						/*for backwards compatibility*/
						NamedConsumerStrategy = NamedConsumerStrategyToCreateProto[settings.ConsumerStrategyName],
#pragma warning restore 612
						ReadBatchSize = settings.ReadBatchSize
					}
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
