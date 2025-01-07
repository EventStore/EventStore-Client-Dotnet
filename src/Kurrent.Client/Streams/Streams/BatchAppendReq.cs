using System;
using Google.Protobuf.WellKnownTypes;

namespace EventStore.Client.Streams {
	partial class BatchAppendReq {
		partial class Types {
			partial class Options {
				public static Options Create(StreamIdentifier streamIdentifier,
					StreamRevision expectedStreamRevision, TimeSpan? timeoutAfter) => new() {
					StreamIdentifier = streamIdentifier,
					StreamPosition = expectedStreamRevision.ToUInt64(),
					Deadline21100 = Timestamp.FromDateTime(timeoutAfter.HasValue
						? DateTime.UtcNow + timeoutAfter.Value
						: DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))
				};
				public static Options Create(StreamIdentifier streamIdentifier, StreamState expectedState,
					TimeSpan? timeoutAfter) => new() {
					StreamIdentifier = streamIdentifier,
					expectedStreamPositionCase_ = expectedState switch {
						{ } when expectedState == StreamState.Any => ExpectedStreamPositionOneofCase.Any,
						{ } when expectedState == StreamState.NoStream => ExpectedStreamPositionOneofCase.NoStream,
						{ } when expectedState == StreamState.StreamExists => ExpectedStreamPositionOneofCase
							.StreamExists,
						_ => ExpectedStreamPositionOneofCase.None
					},
					expectedStreamPosition_ = new Google.Protobuf.WellKnownTypes.Empty(),
					Deadline21100 = Timestamp.FromDateTime(timeoutAfter.HasValue
						? DateTime.UtcNow + timeoutAfter.Value
						: DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))
				};
			}
		}
	}
}
