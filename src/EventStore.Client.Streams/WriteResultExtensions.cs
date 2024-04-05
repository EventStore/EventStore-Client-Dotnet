namespace EventStore.Client;

static class WriteResultExtensions {
	public static IWriteResult OptionallyThrowWrongExpectedVersionException(
		this IWriteResult writeResult,
		EventStoreClientOperationOptions options
	) =>
		(options.ThrowOnAppendFailure, writeResult) switch {
			(true, WrongExpectedVersionResult wrongExpectedVersionResult)
				=> throw new WrongExpectedVersionException(
					wrongExpectedVersionResult.StreamName,
					writeResult.NextExpectedStreamRevision, wrongExpectedVersionResult.ActualStreamRevision
				),
			_ => writeResult
		};
}