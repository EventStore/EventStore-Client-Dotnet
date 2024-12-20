using System;
using Grpc.Core;
using static EventStore.Client.WrongExpectedVersion.CurrentStreamRevisionOptionOneofCase;
using static EventStore.Client.WrongExpectedVersion.ExpectedStreamPositionOptionOneofCase;

namespace EventStore.Client.Streams {
	partial class BatchAppendResp {
		public IWriteResult ToWriteResult() => ResultCase switch {
			ResultOneofCase.Success => new SuccessResult(
				Success.CurrentRevisionOptionCase switch {
					Types.Success.CurrentRevisionOptionOneofCase.CurrentRevision =>
						new StreamRevision(Success.CurrentRevision),
					_ => StreamRevision.None
				}, Success.PositionOptionCase switch {
					Types.Success.PositionOptionOneofCase.Position => new Position(
						Success.Position.CommitPosition,
						Success.Position.PreparePosition),
					_ => Position.End
				}),
			ResultOneofCase.Error => Error.Details switch {
				{ } when Error.Details.Is(WrongExpectedVersion.Descriptor) =>
					FromWrongExpectedVersion(StreamIdentifier, Error.Details.Unpack<WrongExpectedVersion>()),
				{ } when Error.Details.Is(StreamDeleted.Descriptor) =>
					throw new StreamDeletedException(StreamIdentifier!),
				{ } when Error.Details.Is(AccessDenied.Descriptor) => throw new AccessDeniedException(),
				{ } when Error.Details.Is(Timeout.Descriptor) => throw new RpcException(
					new Status(StatusCode.DeadlineExceeded, Error.Message)),
				{ } when Error.Details.Is(Unknown.Descriptor) => throw new InvalidOperationException(Error.Message),
				{ } when Error.Details.Is(MaximumAppendSizeExceeded.Descriptor) =>
					throw new MaximumAppendSizeExceededException(
						Error.Details.Unpack<MaximumAppendSizeExceeded>().MaxAppendSize),
				{ } when Error.Details.Is(BadRequest.Descriptor) => throw new InvalidOperationException(Error.Details
					.Unpack<BadRequest>().Message),
				_ => throw new InvalidOperationException($"Could not recognize {Error.Message}")
			},
			_ => throw new InvalidOperationException()
		};

		private static WrongExpectedVersionResult FromWrongExpectedVersion(StreamIdentifier streamIdentifier,
			WrongExpectedVersion wrongExpectedVersion) => new(streamIdentifier!,
			wrongExpectedVersion.ExpectedStreamPositionOptionCase switch {
				ExpectedStreamPosition => wrongExpectedVersion.ExpectedStreamPosition,
				_ => StreamRevision.None
			}, wrongExpectedVersion.CurrentStreamRevisionOptionCase switch {
				CurrentStreamRevision => wrongExpectedVersion.CurrentStreamRevision,
				_ => StreamRevision.None
			});
	}
}
