#pragma warning disable CS8321 // Local function is declared but never used

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable

await using var client = new EventStoreClient(EventStoreClientSettings.Create("esdb://localhost:2113?tls=false"));

await Task.WhenAll(YieldSamples().Select(async sample => {
	try {
		await sample;
	} catch (OperationCanceledException) { }
}));


return;

IEnumerable<Task> YieldSamples() {
	yield return SubscribeToStream(client, GetCT());
	yield return SubscribeToStreamFromPosition(client, GetCT());
	yield return SubscribeToStreamLive(client, GetCT());
	yield return SubscribeToStreamResolvingLinkTos(client, GetCT());
	yield return SubscribeToStreamSubscriptionDropped(client, GetCT());
	yield return SubscribeToAll(client, GetCT());
	yield return SubscribeToAllFromPosition(client, GetCT());
	yield return SubscribeToAllLive(client, GetCT());
	yield return SubscribeToAllSubscriptionDropped(client, GetCT());
	yield return OverridingUserCredentials(client, GetCT());
}

static async Task SubscribeToStreamFromPosition(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-stream-from-position

	await using var subscription = client.SubscribeToStream(
		"some-stream",
		FromStream.After(StreamPosition.FromInt64(20)),
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);
				break;
		}
	}

	#endregion subscribe-to-stream-from-position
}

static async Task SubscribeToStreamLive(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-stream-live

	await using var subscription = client.SubscribeToStream(
		"some-stream",
		FromStream.End,
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);
				break;
		}
	}

	#endregion subscribe-to-stream-live
}

static async Task SubscribeToStreamResolvingLinkTos(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-stream-resolving-linktos

	await using var subscription = client.SubscribeToStream(
		"$et-myEventType",
		FromStream.Start,
		true,
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);
				break;
		}
	}

	#endregion subscribe-to-stream-resolving-linktos
}

static async Task SubscribeToStreamSubscriptionDropped(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-stream-subscription-dropped

	var checkpoint = await ReadStreamCheckpointAsync() switch {
		null => FromStream.Start,
		var position => FromStream.After(position.Value)
	};

	Subscribe:
	try {
		await using var subscription = client.SubscribeToStream(
			"some-stream",
			checkpoint,
			cancellationToken: ct);
		await foreach (var message in subscription.Messages) {
			switch (message) {
				case StreamMessage.Event(var evnt):
					Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
					await HandleEvent(evnt);
					checkpoint = FromStream.After(evnt.OriginalEventNumber);
					break;
			}
		}
	} catch (OperationCanceledException) {
		Console.WriteLine($"Subscription was canceled.");
	} catch (ObjectDisposedException) {
		Console.WriteLine($"Subscription was canceled by the user.");
	} catch (Exception ex) {
		Console.WriteLine($"Subscription was dropped: {ex}");
		goto Subscribe;
	}

	#endregion subscribe-to-stream-subscription-dropped
}

static async Task SubscribeToStream(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-stream

	await using var subscription = client.SubscribeToStream(
		"some-stream",
		FromStream.Start,
		cancellationToken: ct);
	await foreach (var message in subscription.Messages.WithCancellation(ct)) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);
				break;
		}
	}

	#endregion subscribe-to-stream
}

static async Task SubscribeToAll(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-all

	await using var subscription = client.SubscribeToAll(
		FromAll.Start,
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);
				break;
		}
	}

	#endregion subscribe-to-all
}

static async Task SubscribeToAllFromPosition(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-all-from-position

	var result = await client.AppendToStreamAsync(
		"subscribe-to-all-from-position",
		StreamState.NoStream,
		new[] {
			new EventData(Uuid.NewUuid(), "-", ReadOnlyMemory<byte>.Empty)
		});

	await using var subscription = client.SubscribeToAll(
		FromAll.After(result.LogPosition),
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);
				break;
		}
	}

	#endregion subscribe-to-all-from-position
}

static async Task SubscribeToAllLive(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-all-live

	var subscription = client.SubscribeToAll(
		FromAll.End,
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);
				break;
		}
	}

	#endregion subscribe-to-all-live
}

static async Task SubscribeToAllSubscriptionDropped(EventStoreClient client, CancellationToken ct) {
	#region subscribe-to-all-subscription-dropped

	var checkpoint = await ReadCheckpointAsync() switch {
		null => FromAll.Start,
		var position => FromAll.After(position.Value)
	};

	Subscribe:
	try {
		await using var subscription = client.SubscribeToAll(
			checkpoint,
			cancellationToken: ct);
		await foreach (var message in subscription.Messages) {
			switch (message) {
				case StreamMessage.Event(var evnt):
					Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
					await HandleEvent(evnt);
					if (evnt.OriginalPosition is not null) {
						checkpoint = FromAll.After(evnt.OriginalPosition.Value);
					}

					break;
			}
		}
	} catch (OperationCanceledException) {
		Console.WriteLine($"Subscription was canceled.");
	} catch (ObjectDisposedException) {
		Console.WriteLine($"Subscription was canceled by the user.");
	} catch (Exception ex) {
		Console.WriteLine($"Subscription was dropped: {ex}");
		goto Subscribe;
	}

	#endregion subscribe-to-all-subscription-dropped
}

static async Task SubscribeToFiltered(EventStoreClient client, CancellationToken ct) {
	#region stream-prefix-filtered-subscription

	var prefixStreamFilter = new SubscriptionFilterOptions(StreamFilter.Prefix("test-", "other-"));
	await using var subscription = client.SubscribeToAll(
		FromAll.Start,
		filterOptions: prefixStreamFilter,
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);

				break;
			case StreamMessage.AllStreamCheckpointReached(var position):
				Console.WriteLine($"Checkpoint reached: {position}");
				break;
		}
	}

	#endregion stream-prefix-filtered-subscription

	#region stream-regex-filtered-subscription

	var regexStreamFilter = StreamFilter.RegularExpression(@"/invoice-\d\d\d/g");

	#endregion stream-regex-filtered-subscription
}

static async Task OverridingUserCredentials(EventStoreClient client, CancellationToken ct) {
	#region overriding-user-credentials

	await using var subscription = client.SubscribeToAll(
		FromAll.Start,
		userCredentials: new UserCredentials("admin", "changeit"),
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var evnt):
				Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
				await HandleEvent(evnt);

				break;
		}
	}

	#endregion overriding-user-credentials
}

static Task HandleEvent(ResolvedEvent evnt) => Task.CompletedTask;

static Task<StreamPosition?> ReadStreamCheckpointAsync() =>
	Task.FromResult(new StreamPosition?());

static Task<Position?> ReadCheckpointAsync() =>
	Task.FromResult(new Position?());

// ensures that samples exit in a timely manner on CI
static CancellationToken GetCT() => new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
