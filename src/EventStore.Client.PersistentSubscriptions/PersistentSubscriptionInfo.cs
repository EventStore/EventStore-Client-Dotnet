using EventStore.Client.PersistentSubscriptions;
using Google.Protobuf.Collections;

namespace EventStore.Client;

/// <summary>
/// Provides the details for a persistent subscription.
/// </summary>
/// <param name="EventSource">The source of events for the subscription.</param>
/// <param name="GroupName">The group name given on creation.</param>
/// <param name="Status">The current status of the subscription.</param>
/// <param name="Connections">Active connections to the subscription.</param>
/// <param name="Settings">The settings used to create the persistant subscription.</param>
/// <param name="Stats">Live statistics for the persistent subscription.</param>
public record PersistentSubscriptionInfo(
	string EventSource,
	string GroupName,
	string Status,
	IEnumerable<PersistentSubscriptionConnectionInfo> Connections,
	PersistentSubscriptionSettings? Settings,
	PersistentSubscriptionStats Stats
) {
	internal static PersistentSubscriptionInfo From(SubscriptionInfo info) {
		IPosition? startFrom                     = null;
		IPosition? lastCheckpointedEventPosition = null;
		IPosition? lastKnownEventPosition        = null;
		
		if (info.EventSource == SystemStreams.AllStream) {
			if (Position.TryParse(info.StartFrom, out var position)) startFrom                                     = position;
			if (Position.TryParse(info.LastCheckpointedEventPosition, out position)) lastCheckpointedEventPosition = position;
			if (Position.TryParse(info.LastKnownEventPosition, out position)) lastKnownEventPosition               = position;
		}
		else {
			if (long.TryParse(info.StartFrom, out var streamPosition)) startFrom                                    = StreamPosition.FromInt64(streamPosition);
			if (ulong.TryParse(info.LastCheckpointedEventPosition, out var position)) lastCheckpointedEventPosition = new StreamPosition(position);
			if (ulong.TryParse(info.LastKnownEventPosition, out position)) lastKnownEventPosition                   = new StreamPosition(position);
		}

		return new PersistentSubscriptionInfo(
			info.EventSource,
			info.GroupName,
			info.Status,
			From(info.Connections),
			new PersistentSubscriptionSettings(
				info.ResolveLinkTos,
				startFrom,
				info.ExtraStatistics,
				TimeSpan.FromMilliseconds(info.MessageTimeoutMilliseconds),
				info.MaxRetryCount,
				info.LiveBufferSize,
				info.ReadBatchSize,
				info.BufferSize,
				TimeSpan.FromMilliseconds(info.CheckPointAfterMilliseconds),
				info.MinCheckPointCount,
				info.MaxCheckPointCount,
				info.MaxSubscriberCount,
				info.NamedConsumerStrategy
			),
			new PersistentSubscriptionStats(
				info.AveragePerSecond,
				info.TotalItems,
				info.CountSinceLastMeasurement,
				info.ReadBufferCount,
				info.LiveBufferCount,
				info.RetryBufferCount,
				info.TotalInFlightMessages,
				info.OutstandingMessagesCount,
				info.ParkedMessageCount,
				lastCheckpointedEventPosition,
				lastKnownEventPosition
			)
		);
	}

	internal static PersistentSubscriptionInfo From(PersistentSubscriptionDto info) {
		PersistentSubscriptionSettings? settings = null;
		if (info.Config != null)
			settings = new PersistentSubscriptionSettings(
				info.Config.ResolveLinktos,
				// we only need to support StreamPosition as $all was never implemented in http api.
				new StreamPosition(info.Config.StartFrom),
				info.Config.ExtraStatistics,
				TimeSpan.FromMilliseconds(info.Config.MessageTimeoutMilliseconds),
				info.Config.MaxRetryCount,
				info.Config.LiveBufferSize,
				info.Config.ReadBatchSize,
				info.Config.BufferSize,
				TimeSpan.FromMilliseconds(info.Config.CheckPointAfterMilliseconds),
				info.Config.MinCheckPointCount,
				info.Config.MaxCheckPointCount,
				info.Config.MaxSubscriberCount,
				info.Config.NamedConsumerStrategy
			);

		return new PersistentSubscriptionInfo(
			info.EventStreamId,
			info.GroupName,
			info.Status,
			PersistentSubscriptionConnectionInfo.CreateFrom(info.Connections),
			settings,
			new PersistentSubscriptionStats(
				(int)info.AverageItemsPerSecond,
				info.TotalItemsProcessed,
				info.CountSinceLastMeasurement,
				info.ReadBufferCount,
				info.LiveBufferCount,
				info.RetryBufferCount,
				info.TotalInFlightMessages,
				info.OutstandingMessagesCount,
				info.ParkedMessageCount,
				StreamPosition.FromInt64(info.LastProcessedEventNumber),
				StreamPosition.FromInt64(info.LastKnownEventNumber)
			)
		);
	}

	static IEnumerable<PersistentSubscriptionConnectionInfo> From(
		RepeatedField<SubscriptionInfo.Types.ConnectionInfo> connections
	) {
		foreach (var conn in connections)
			yield return new PersistentSubscriptionConnectionInfo(
				conn.From,
				conn.Username,
				AverageItemsPerSecond: conn.AverageItemsPerSecond,
				TotalItems: conn.TotalItems,
				CountSinceLastMeasurement: conn.CountSinceLastMeasurement,
				AvailableSlots: conn.AvailableSlots,
				InFlightMessages: conn.InFlightMessages,
				ConnectionName: conn.ConnectionName,
				ExtraStatistics: From(conn.ObservedMeasurements)
			);
	}

	static IDictionary<string, long> From(IEnumerable<SubscriptionInfo.Types.Measurement> measurements) =>
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
	int AveragePerSecond,
	long TotalItems,
	long CountSinceLastMeasurement,
	int ReadBufferCount,
	long LiveBufferCount,
	int RetryBufferCount,
	int TotalInFlightMessages,
	int OutstandingMessagesCount,
	long ParkedMessageCount,
	IPosition? LastCheckpointedEventPosition,
	IPosition? LastKnownEventPosition
);

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
public record PersistentSubscriptionConnectionInfo(
	string From,
	string Username,
	string ConnectionName,
	int AverageItemsPerSecond,
	long TotalItems,
	long CountSinceLastMeasurement,
	int AvailableSlots,
	int InFlightMessages,
	IDictionary<string, long> ExtraStatistics
) {
	internal static IEnumerable<PersistentSubscriptionConnectionInfo> CreateFrom(
		IEnumerable<PersistentSubscriptionConnectionInfoDto> connections
	) {
		foreach (var connection in connections) yield return CreateFrom(connection);
	}

	static PersistentSubscriptionConnectionInfo CreateFrom(PersistentSubscriptionConnectionInfoDto connection) =>
		new(
			connection.From,
			connection.Username,
			connection.ConnectionName,
			(int)connection.AverageItemsPerSecond,
			connection.TotalItems,
			connection.CountSinceLastMeasurement,
			connection.AvailableSlots,
			connection.InFlightMessages,
			CreateFrom(connection.ExtraStatistics)
		);

	static IDictionary<string, long> CreateFrom(IEnumerable<PersistentSubscriptionMeasurementInfoDto> extraStatistics) =>
		extraStatistics.ToDictionary(k => k.Key, v => v.Value);
}

record PersistentSubscriptionDto(
	string EventStreamId,
	string GroupName,
	string Status,
	float AverageItemsPerSecond,
	long TotalItemsProcessed,
	long CountSinceLastMeasurement,
	long LastProcessedEventNumber,
	long LastKnownEventNumber,
	int ReadBufferCount,
	long LiveBufferCount,
	int RetryBufferCount,
	int TotalInFlightMessages,
	int OutstandingMessagesCount,
	int ParkedMessageCount,
	PersistentSubscriptionConfig? Config,
	IEnumerable<PersistentSubscriptionConnectionInfoDto> Connections
);

record PersistentSubscriptionConfig(
	bool ResolveLinktos,
	ulong StartFrom,
	string StartPosition,
	int MessageTimeoutMilliseconds,
	bool ExtraStatistics,
	int MaxRetryCount,
	int LiveBufferSize,
	int BufferSize,
	int ReadBatchSize,
	int CheckPointAfterMilliseconds,
	int MinCheckPointCount,
	int MaxCheckPointCount,
	int MaxSubscriberCount,
	string NamedConsumerStrategy
);

record PersistentSubscriptionConnectionInfoDto(
	string From,
	string Username,
	float AverageItemsPerSecond,
	long TotalItems,
	long CountSinceLastMeasurement,
	int AvailableSlots,
	int InFlightMessages,
	string ConnectionName,
	IEnumerable<PersistentSubscriptionMeasurementInfoDto> ExtraStatistics
);

record PersistentSubscriptionMeasurementInfoDto(string Key, long Value);