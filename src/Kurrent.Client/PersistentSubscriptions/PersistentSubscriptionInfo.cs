using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Client.PersistentSubscriptions;
using Google.Protobuf.Collections;

namespace EventStore.Client {
	/// <summary>
	/// Provides the details for a persistent subscription.
	/// </summary>
	/// <param name="EventSource">The source of events for the subscription.</param>
	/// <param name="GroupName">The group name given on creation.</param>
	/// <param name="Status">The current status of the subscription.</param>
	/// <param name="Connections">Active connections to the subscription.</param>
	/// <param name="Settings">The settings used to create the persistant subscription.</param>
	/// <param name="Stats">Live statistics for the persistent subscription.</param>
	public record PersistentSubscriptionInfo(string EventSource, string GroupName, string Status,
		IEnumerable<PersistentSubscriptionConnectionInfo> Connections,
		PersistentSubscriptionSettings? Settings, PersistentSubscriptionStats Stats) {

		internal static PersistentSubscriptionInfo From(SubscriptionInfo info) {
			IPosition? startFrom = null;
			IPosition? lastCheckpointedEventPosition = null;
			IPosition? lastKnownEventPosition = null;
			if (info.EventSource == SystemStreams.AllStream) {
				if (Position.TryParse(info.StartFrom, out var position)) {
					startFrom = position;
				}
				if (Position.TryParse(info.LastCheckpointedEventPosition, out position)) {
					lastCheckpointedEventPosition = position;
				}
				if (Position.TryParse(info.LastKnownEventPosition, out position)) {
					lastKnownEventPosition = position;
				}
			} else {
				if (long.TryParse(info.StartFrom, out var streamPosition)) {
					startFrom = StreamPosition.FromInt64(streamPosition);
				}
				if (ulong.TryParse(info.LastCheckpointedEventPosition, out var position)) {
					lastCheckpointedEventPosition = new StreamPosition(position);
				}
				if (ulong.TryParse(info.LastKnownEventPosition, out position)) {
					lastKnownEventPosition = new StreamPosition(position);
				}
			}
			
			return new PersistentSubscriptionInfo(
				EventSource: info.EventSource,
				GroupName: info.GroupName,
				Status: info.Status,
				Connections: From(info.Connections),
				Settings: new PersistentSubscriptionSettings(
					resolveLinkTos: info.ResolveLinkTos,
					startFrom: startFrom,
					extraStatistics: info.ExtraStatistics,
					messageTimeout: TimeSpan.FromMilliseconds(info.MessageTimeoutMilliseconds),
					maxRetryCount: info.MaxRetryCount,
					liveBufferSize: info.LiveBufferSize,
					readBatchSize: info.ReadBatchSize,
					historyBufferSize: info.BufferSize,
					checkPointAfter: TimeSpan.FromMilliseconds(info.CheckPointAfterMilliseconds),
					checkPointLowerBound: info.MinCheckPointCount,
					checkPointUpperBound: info.MaxCheckPointCount,
					maxSubscriberCount: info.MaxSubscriberCount,
					consumerStrategyName: info.NamedConsumerStrategy
				),
				Stats: new PersistentSubscriptionStats(
					AveragePerSecond: info.AveragePerSecond,
					TotalItems: info.TotalItems,
					CountSinceLastMeasurement: info.CountSinceLastMeasurement,
					ReadBufferCount: info.ReadBufferCount,
					LiveBufferCount: info.LiveBufferCount,
					RetryBufferCount: info.RetryBufferCount,
					TotalInFlightMessages: info.TotalInFlightMessages,
					OutstandingMessagesCount: info.OutstandingMessagesCount,
					ParkedMessageCount: info.ParkedMessageCount,
					LastCheckpointedEventPosition: lastCheckpointedEventPosition,
					LastKnownEventPosition: lastKnownEventPosition
				)
			);
		}

		internal static PersistentSubscriptionInfo From(PersistentSubscriptionDto info) {
			PersistentSubscriptionSettings? settings = null;
			if (info.Config != null) {
				settings = new PersistentSubscriptionSettings(
					resolveLinkTos: info.Config.ResolveLinktos,
					// we only need to support StreamPosition as $all was never implemented in http api.
					startFrom: new StreamPosition(info.Config.StartFrom),
					extraStatistics: info.Config.ExtraStatistics,
					messageTimeout: TimeSpan.FromMilliseconds(info.Config.MessageTimeoutMilliseconds),
					maxRetryCount: info.Config.MaxRetryCount,
					liveBufferSize: info.Config.LiveBufferSize,
					readBatchSize: info.Config.ReadBatchSize,
					historyBufferSize: info.Config.BufferSize,
					checkPointAfter: TimeSpan.FromMilliseconds(info.Config.CheckPointAfterMilliseconds),
					checkPointLowerBound: info.Config.MinCheckPointCount,
					checkPointUpperBound: info.Config.MaxCheckPointCount,
					maxSubscriberCount: info.Config.MaxSubscriberCount,
					consumerStrategyName: info.Config.NamedConsumerStrategy
				);
			}
			
			return new PersistentSubscriptionInfo(
				EventSource: info.EventStreamId,
				GroupName: info.GroupName,
				Status: info.Status,
				Connections: PersistentSubscriptionConnectionInfo.CreateFrom(info.Connections),
				Settings: settings,
				Stats: new PersistentSubscriptionStats(
					AveragePerSecond: (int)info.AverageItemsPerSecond,
					TotalItems: info.TotalItemsProcessed,
					CountSinceLastMeasurement: info.CountSinceLastMeasurement,
					ReadBufferCount: info.ReadBufferCount,
					LiveBufferCount: info.LiveBufferCount,
					RetryBufferCount: info.RetryBufferCount,
					TotalInFlightMessages: info.TotalInFlightMessages,
					OutstandingMessagesCount: info.OutstandingMessagesCount,
					ParkedMessageCount: info.ParkedMessageCount,
					LastCheckpointedEventPosition: StreamPosition.FromInt64(info.LastProcessedEventNumber),
					LastKnownEventPosition: StreamPosition.FromInt64(info.LastKnownEventNumber)
				)
			);
		}

		private static IEnumerable<PersistentSubscriptionConnectionInfo> From(
			RepeatedField<SubscriptionInfo.Types.ConnectionInfo> connections) {
			foreach (var conn in connections) {
				yield return new PersistentSubscriptionConnectionInfo(
					From: conn.From,
					Username: conn.Username,
					AverageItemsPerSecond: conn.AverageItemsPerSecond,
					TotalItems: conn.TotalItems,
					CountSinceLastMeasurement: conn.CountSinceLastMeasurement,
					AvailableSlots: conn.AvailableSlots,
					InFlightMessages: conn.InFlightMessages,
					ConnectionName: conn.ConnectionName,
					ExtraStatistics: From(conn.ObservedMeasurements)
				);
			}
		}

		private static IDictionary<string, long> From(IEnumerable<SubscriptionInfo.Types.Measurement> measurements) =>
			measurements.ToDictionary(k => k.Key, v => v.Value);
	}

	/// <summary>
	/// Provides the statistics of a persistent subscription.
	/// </summary>
	/// <param name="AveragePerSecond">Average number of events per second.</param>
	/// <param name="TotalItems">Total number of events processed by subscription.</param>
	/// <param name="CountSinceLastMeasurement">Number of events seen since last measurement on this connection (used as the basis for <see cref="AveragePerSecond"/>).</param>
	/// <param name="ReadBufferCount">Number of events in the read buffer.</param>
	/// <param name="LiveBufferCount">Number of events in the live buffer.</param>
	/// <param name="RetryBufferCount">Number of events in the retry buffer.</param>
	/// <param name="TotalInFlightMessages">Current in flight messages across all connections.</param>
	/// <param name="OutstandingMessagesCount">Current number of outstanding messages.</param>
	/// <param name="ParkedMessageCount">The current number of parked messages.</param>
	/// <param name="LastCheckpointedEventPosition">The <see cref="IPosition"/> of the last checkpoint. This will be null if there are no checkpoints.</param>
	/// <param name="LastKnownEventPosition">The <see cref="IPosition"/> of the last known event. This will be undefined if no events have been received yet.</param>
	public record PersistentSubscriptionStats(
		int AveragePerSecond, long TotalItems, long CountSinceLastMeasurement, int ReadBufferCount,
		long LiveBufferCount, int RetryBufferCount, int TotalInFlightMessages, int OutstandingMessagesCount,
		long ParkedMessageCount, IPosition? LastCheckpointedEventPosition, IPosition? LastKnownEventPosition);

	/// <summary>
	/// Provides the details of a persistent subscription connection.
	/// </summary>
	/// <param name="From">Origin of this connection.</param>
	/// <param name="Username">Connection username.</param>
	/// <param name="ConnectionName">The name of the connection.</param>
	/// <param name="AverageItemsPerSecond">Average events per second on this connection.</param>
	/// <param name="TotalItems">Total items on this connection.</param>
	/// <param name="CountSinceLastMeasurement">Number of items seen since last measurement on this connection (used as the basis for averageItemsPerSecond).</param>
	/// <param name="AvailableSlots">Number of available slots.</param>
	/// <param name="InFlightMessages">Number of in flight messages on this connection.</param>
	/// <param name="ExtraStatistics">Timing measurements for the connection. Can be enabled with the ExtraStatistics setting.</param>
	public record PersistentSubscriptionConnectionInfo(string From, string Username, string ConnectionName, int AverageItemsPerSecond,
		long TotalItems, long CountSinceLastMeasurement, int AvailableSlots, int InFlightMessages,
		IDictionary<string, long> ExtraStatistics) {

		internal static IEnumerable<PersistentSubscriptionConnectionInfo> CreateFrom(
			IEnumerable<PersistentSubscriptionConnectionInfoDto> connections) {

			foreach (var connection in connections) {
				yield return  CreateFrom(connection);
			}
		}

		private static PersistentSubscriptionConnectionInfo CreateFrom(PersistentSubscriptionConnectionInfoDto connection) {
			return  new PersistentSubscriptionConnectionInfo(
				From: connection.From,
				Username: connection.Username,
				ConnectionName: connection.ConnectionName,
				AverageItemsPerSecond: (int)connection.AverageItemsPerSecond,
				TotalItems: connection.TotalItems,
				CountSinceLastMeasurement: connection.CountSinceLastMeasurement,
				AvailableSlots: connection.AvailableSlots,
				InFlightMessages: connection.InFlightMessages,
				ExtraStatistics: CreateFrom(connection.ExtraStatistics)
			);
		}

		private static IDictionary<string, long> CreateFrom(IEnumerable<PersistentSubscriptionMeasurementInfoDto> extraStatistics) =>
			extraStatistics.ToDictionary(k => k.Key, v => v.Value);
	}
	
	internal record PersistentSubscriptionDto(string EventStreamId, string GroupName,
		string Status, float AverageItemsPerSecond, long TotalItemsProcessed, long CountSinceLastMeasurement,
		long LastProcessedEventNumber, long LastKnownEventNumber, int ReadBufferCount, long LiveBufferCount,
		int RetryBufferCount, int TotalInFlightMessages, int OutstandingMessagesCount, int ParkedMessageCount,
		PersistentSubscriptionConfig? Config, IEnumerable<PersistentSubscriptionConnectionInfoDto> Connections);

	internal record PersistentSubscriptionConfig(bool ResolveLinktos, ulong StartFrom, string StartPosition,
		int MessageTimeoutMilliseconds, bool ExtraStatistics, int MaxRetryCount, int LiveBufferSize, int BufferSize,
		int ReadBatchSize, int CheckPointAfterMilliseconds, int MinCheckPointCount, int MaxCheckPointCount,
		int MaxSubscriberCount, string NamedConsumerStrategy);
	
	internal record PersistentSubscriptionConnectionInfoDto(string From, string Username, float AverageItemsPerSecond,
		long TotalItems, long CountSinceLastMeasurement, int AvailableSlots, int InFlightMessages, string ConnectionName,
		IEnumerable<PersistentSubscriptionMeasurementInfoDto> ExtraStatistics);
	
	internal record PersistentSubscriptionMeasurementInfoDto(string Key, long Value);
}
