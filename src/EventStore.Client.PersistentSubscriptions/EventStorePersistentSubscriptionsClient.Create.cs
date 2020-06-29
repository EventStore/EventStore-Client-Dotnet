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

			await _client.CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					StreamIdentifier = streamName,
					GroupName = groupName,
					Settings = new CreateReq.Types.Settings {
						Revision = settings.StartFrom,
						CheckpointAfter = settings.CheckPointAfter.Ticks,
						ExtraStatistics = settings.ExtraStatistics,
						MessageTimeout = settings.MessageTimeout.Ticks,
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
