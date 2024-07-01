// ReSharper disable InconsistentNaming

using EventStore.Client.Streams.Tests.Subscriptions;

namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll.Obsolete;

[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class
	PersistentSubscriptionDropsDueToCancellationToken(PersistentSubscriptionDropsDueToCancellationToken.Fixture fixture)
	: IClassFixture<
		PersistentSubscriptionDropsDueToCancellationToken.Fixture> {
	static readonly string Group  = Guid.NewGuid().ToString();
	static readonly string Stream = Guid.NewGuid().ToString();

	[SupportsPSToAll.Fact]
	public async Task persistent_subscription_to_all_drops_due_to_cancellation_token() {
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		await fixture.Client.CreateToAllAsync(
			Group,
			cancellationToken: cts.Token,
			settings: new PersistentSubscriptionSettings()
		);

		using var subscription = await fixture.Client.SubscribeToAllAsync(
				Group,
				async (s, e, r, ct) => await s.Ack(e),
				(sub, reason, ex) => subscriptionDropped.SetResult(new SubscriptionDroppedResult(reason, ex)),
				userCredentials: TestCredentials.Root,
				cancellationToken: cts.Token
			)
			.WithTimeout();

		// wait until the cancellation token cancels
		await Task.Delay(TimeSpan.FromSeconds(3));

		var result = await subscriptionDropped.Task.WithTimeout();
		result.Reason.ShouldBe(SubscriptionDroppedReason.Disposed);
	}

	[SupportsPSToAll.Fact]
	public async Task persistent_subscription_to_stream_drops_due_to_cancellation_token() {
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		await fixture.Client.CreateToStreamAsync(
			Group,
			Stream,
			cancellationToken: cts.Token,
			settings: new PersistentSubscriptionSettings()
		);

		using var subscription = await fixture.Client.SubscribeToStreamAsync(
				Group,
				Stream,
				async (s, e, r, ct) => await s.Ack(e),
				(sub, reason, ex) => subscriptionDropped.SetResult(new SubscriptionDroppedResult(reason, ex)),
				userCredentials: TestCredentials.Root,
				cancellationToken: cts.Token
			)
			.WithTimeout();

		// wait until the cancellation token cancels
		await Task.Delay(TimeSpan.FromSeconds(3));

		var result = await subscriptionDropped.Task.WithTimeout();
		result.Reason.ShouldBe(SubscriptionDroppedReason.Disposed);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() {
			return Task.CompletedTask;
		}

		protected override Task When() => Task.CompletedTask;
	}
}
