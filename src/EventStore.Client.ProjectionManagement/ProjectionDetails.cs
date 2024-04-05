namespace EventStore.Client;

/// <summary>
/// Provides the details for a projection.
/// </summary>
/// <param name="CoreProcessingTime">The core processing time of the projection.</param>
/// <param name="Version">The version of the projection.</param>
/// <param name="Epoch">The epoch of the projection.</param>
/// <param name="EffectiveName">The effective name of the projection.</param>
/// <param name="WritesInProgress">The number of writes in progress in the projection.</param>
/// <param name="ReadsInProgress">The number of reads in progress in the projection.</param>
/// <param name="PartitionsCached">The number of partitions cached in the projection.</param>
/// <param name="Status">The status of the projection.</param>
/// <param name="StateReason">The reason for the state of the projection.</param>
/// <param name="Name">The name of the projection.</param>
/// <param name="Mode">The mode of the projection.</param>
/// <param name="Position">The position of the projection.</param>
/// <param name="Progress">The progress of the projection.</param>
/// <param name="LastCheckpoint">The last checkpoint of the projection.</param>
/// <param name="EventsProcessedAfterRestart">The number of events processed after restart in the projection.</param>
/// <param name="CheckpointStatus">The checkpoint status of the projection.</param>
/// <param name="BufferedEvents">The number of buffered events in the projection.</param>
/// <param name="WritePendingEventsBeforeCheckpoint">The number of write pending events before checkpoint in the projection.</param>
/// <param name="WritePendingEventsAfterCheckpoint">The number of write pending events after checkpoint in the projection.</param>
public record ProjectionDetails(
    long CoreProcessingTime,
    long Version,
    long Epoch,
    string EffectiveName,
    int WritesInProgress,
    int ReadsInProgress,
    int PartitionsCached,
    string Status,
    string StateReason,
    string Name,
    string Mode,
    string Position,
    float Progress,
    string LastCheckpoint,
    long EventsProcessedAfterRestart,
    string CheckpointStatus,
    long BufferedEvents,
    int WritePendingEventsBeforeCheckpoint,
    int WritePendingEventsAfterCheckpoint
);
