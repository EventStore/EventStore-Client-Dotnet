namespace EventStore.Client.Streams.Tests.Subscriptions;

public record SubscriptionDroppedResult(SubscriptionDroppedReason Reason, Exception? Error) {
	public Task Throw() => Task.FromException(Error!);

	public static SubscriptionDroppedResult ServerError(Exception? error = null) =>
		new(SubscriptionDroppedReason.ServerError, error ?? new Exception("Server error"));

	public static SubscriptionDroppedResult SubscriberError(Exception? error = null) => 
		new(SubscriptionDroppedReason.SubscriberError, error ?? new Exception("Subscriber error"));

	public static SubscriptionDroppedResult Disposed(Exception? error = null) => 
		new(SubscriptionDroppedReason.Disposed, error);

	public override string ToString() => $"{Reason} {Error?.Message ?? string.Empty}".Trim();
}