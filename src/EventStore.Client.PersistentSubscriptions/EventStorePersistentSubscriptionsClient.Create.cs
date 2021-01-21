using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;

#nullable enable
namespace EventStore.Client {
	partial class EventStorePersistentSubscriptionsClient {
		private static readonly IDictionary<string, CreateReq.Types.ConsumerStrategy> NamedConsumerStrategyToCreateProto
			= new Dictionary<string, CreateReq.Types.ConsumerStrategy> {
				[SystemConsumerStrategies.DispatchToSingle] = CreateReq.Types.ConsumerStrategy.DispatchToSingle,
				[SystemConsumerStrategies.RoundRobin] = CreateReq.Types.ConsumerStrategy.RoundRobin,
				[SystemConsumerStrategies.Pinned] = CreateReq.Types.ConsumerStrategy.Pinned,
			};

		/// <summary>
		/// Creates a persistent subscription.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="groupName"></param>
		/// <param name="settings"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task CreateAsync(string streamName, string groupName,
			PersistentSubscriptionSettings settings, UserCredentials? userCredentials = null,
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

			await new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					StreamIdentifier = streamName,
					GroupName = groupName,
					Settings = new CreateReq.Types.Settings {
						Revision = settings.StartFrom,
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
						NamedConsumerStrategy = NamedConsumerStrategyToCreateProto[settings.NamedConsumerStrategy],
						ReadBatchSize = settings.ReadBatchSize
					}
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}
	}
}
