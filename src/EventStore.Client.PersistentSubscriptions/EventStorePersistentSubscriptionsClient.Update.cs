using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;

#nullable enable
namespace EventStore.Client {
	public partial class EventStorePersistentSubscriptionsClient {
		private static readonly IDictionary<string, UpdateReq.Types.ConsumerStrategy> NamedConsumerStrategyToUpdateProto
			= new Dictionary<string, UpdateReq.Types.ConsumerStrategy> {
				[SystemConsumerStrategies.DispatchToSingle] = UpdateReq.Types.ConsumerStrategy.DispatchToSingle,
				[SystemConsumerStrategies.RoundRobin] = UpdateReq.Types.ConsumerStrategy.RoundRobin,
				[SystemConsumerStrategies.Pinned] = UpdateReq.Types.ConsumerStrategy.Pinned,
			};

		private static UpdateReq.Types.StreamOptions StreamOptionsForUpdateProto(string streamName,
			StreamPosition position) {
			if (position == StreamPosition.Start) {
				return new UpdateReq.Types.StreamOptions {
					StreamIdentifier = streamName,
					Start = new Empty()
				};
			}

			if (position == StreamPosition.End) {
				return new UpdateReq.Types.StreamOptions {
					StreamIdentifier = streamName,
					End = new Empty()
				};
			}

			return new UpdateReq.Types.StreamOptions {
				StreamIdentifier = streamName,
				Revision = position.ToUInt64()
			};
		}

		private static UpdateReq.Types.AllOptions AllOptionsForUpdateProto(Position position) {
			if (position == Position.Start) {
				return new UpdateReq.Types.AllOptions {
					Start = new Empty()
				};
			}

			if (position == Position.End) {
				return new UpdateReq.Types.AllOptions {
					End = new Empty()
				};
			}

			return new UpdateReq.Types.AllOptions {
				Position = new UpdateReq.Types.Position {
					CommitPosition = position.CommitPosition,
					PreparePosition = position.PreparePosition
				}
			};
		}


		/// <summary>
		/// Updates a persistent subscription.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="groupName"></param>
		/// <param name="settings"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task UpdateAsync(string streamName, string groupName, PersistentSubscriptionSettings settings,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (streamName == null) {
				throw new ArgumentNullException(nameof(streamName));
			}

			if (groupName == null) {
				throw new ArgumentNullException(nameof(groupName));
			}

			if (settings == null) {
				throw new ArgumentNullException(nameof(settings));
			}

			if (streamName != SystemStreams.AllStream && settings.StartFrom != null &&
			    !(settings.StartFrom is StreamPosition)) {
				throw new ArgumentException(
					$"{nameof(settings.StartFrom)} must be of type '{nameof(StreamPosition)}' when subscribing to a stream");
			}

			if (streamName == SystemStreams.AllStream && settings.StartFrom != null &&
			    !(settings.StartFrom is Position)) {
				throw new ArgumentException(
					$"{nameof(settings.StartFrom)} must be of type '{nameof(Position)}' when subscribing to {SystemStreams.AllStream}");
			}

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);

			if (streamName == SystemStreams.AllStream &&
			    !channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll) {
				throw new NotSupportedException("The server does not support persistent subscriptions to $all.");
			}

			using var call = new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(channelInfo.CallInvoker)
				.UpdateAsync(new UpdateReq {
						Options = new UpdateReq.Types.Options {
							GroupName = groupName,
							Stream = streamName != SystemStreams.AllStream
								? StreamOptionsForUpdateProto(streamName,
									(StreamPosition)(settings.StartFrom ?? StreamPosition.End))
								: null,
							All = streamName == SystemStreams.AllStream
								? AllOptionsForUpdateProto((Position)(settings.StartFrom ?? Position.End))
								: null,
#pragma warning disable 612
							StreamIdentifier =
								streamName != SystemStreams.AllStream
									? streamName
									: string.Empty, /*for backwards compatibility*/
#pragma warning restore 612
							Settings = new UpdateReq.Types.Settings {
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
								MaxCheckpointCount = settings.MaxCheckPointCount,
								MaxRetryCount = settings.MaxRetryCount,
								MaxSubscriberCount = settings.MaxSubscriberCount,
								MinCheckpointCount = settings.MinCheckPointCount,
								NamedConsumerStrategy =
									NamedConsumerStrategyToUpdateProto[settings.NamedConsumerStrategy],
								ReadBatchSize = settings.ReadBatchSize
							}
						}
					},
					EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials,
						cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Updates a persistent subscription to $all.
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="settings"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task UpdateToAllAsync(string groupName, PersistentSubscriptionSettings settings,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			await UpdateAsync(
					streamName: SystemStreams.AllStream,
					groupName: groupName,
					settings: settings,
					userCredentials: userCredentials,
					cancellationToken: cancellationToken)
				.ConfigureAwait(false);
	}
}
