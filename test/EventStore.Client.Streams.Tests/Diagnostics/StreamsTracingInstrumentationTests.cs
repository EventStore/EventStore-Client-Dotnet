using EventStore.Client.Diagnostics;
using EventStore.Diagnostics.Tracing;

namespace EventStore.Client.Streams.Tests.Diagnostics;

[Trait("Category", "Diagnostics:Tracing")]
public class StreamsTracingInstrumentationTests(ITestOutputHelper output, DiagnosticsFixture fixture)
	: EventStoreTests<DiagnosticsFixture>(output, fixture) {
	[Fact]
	public async Task AppendIsInstrumentedWithTracingAsExpected() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		var activity = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, stream)
			.SingleOrDefault()
			.ShouldNotBeNull();

		Fixture.AssertAppendActivityHasExpectedTags(activity, stream);
	}

	[Fact]
	public async Task AppendTraceIsTaggedWithErrorStatusOnException() {
		var stream = Fixture.GetStreamName();

		var actualException = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEventsThatThrowsException()
		).ShouldThrowAsync<Exception>();

		var activity = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, stream)
			.SingleOrDefault()
			.ShouldNotBeNull();

		Fixture.AssertErroneousAppendActivityHasExpectedTags(activity, actualException);
	}

	[Fact]
	public async Task TracingContextIsInjectedWhenUserMetadataIsValidJsonObject() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1, metadata: Fixture.CreateTestJsonMetadata())
		);

		var activity = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, stream)
			.SingleOrDefault()
			.ShouldNotBeNull();

		var readResult = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		var tracingMetadata = readResult[0].OriginalEvent.Metadata.ExtractTracingMetadata();

		tracingMetadata.ShouldNotBe(TracingMetadata.None);
		tracingMetadata.TraceId.ShouldBe(activity.TraceId.ToString());
		tracingMetadata.SpanId.ShouldBe(activity.SpanId.ToString());
	}

	[Fact]
	public async Task TracingContextIsNotInjectedWhenUserMetadataIsNotValidJsonObject() {
		var stream = Fixture.GetStreamName();

		var inputMetadata = "clearlynotavalidjsonobject"u8.ToArray();
		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1, metadata: inputMetadata)
		);

		var readResult = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		var outputMetadata = readResult[0].OriginalEvent.Metadata.ToArray();
		outputMetadata.ShouldBe(inputMetadata);
	}

	[Fact]
	public async Task TracingContextIsInjectedWhenEventIsNotJsonButHasJsonMetadata() {
		var stream = Fixture.GetStreamName();

		var inputMetadata = Fixture.CreateTestJsonMetadata().ToArray();
		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(
				metadata: inputMetadata,
				contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
			)
		);

		var readResult = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		var outputMetadata = readResult[0].OriginalEvent.Metadata.ToArray();
		outputMetadata.ShouldNotBe(inputMetadata);

		var appendActivities = Fixture.GetActivitiesForOperation(TracingConstants.Operations.Append, stream);

		appendActivities.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task json_metadata_event_is_traced_and_non_json_metadata_event_is_not_traced() {
		var streamName = Fixture.GetStreamName();

		var seedEvents = new[] {
			Fixture.CreateTestEvent(metadata: Fixture.CreateTestJsonMetadata()),
			Fixture.CreateTestEvent(metadata: Fixture.CreateTestNonJsonMetadata())
		};

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await using var subscription = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		var appendActivities = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, streamName)
			.ShouldNotBeNull();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Subscribe(enumerator).WithTimeout();

		var subscribeActivities = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Subscribe, streamName)
			.ToArray();

		appendActivities.ShouldHaveSingleItem();

		subscribeActivities.ShouldHaveSingleItem();

		subscribeActivities.First().ParentId.ShouldBe(appendActivities.First().Id);

		var jsonMetadataEvent = seedEvents.First();

		Fixture.AssertSubscriptionActivityHasExpectedTags(
			subscribeActivities.First(),
			streamName,
			jsonMetadataEvent.EventId.ToString()
		);

		return;

		async Task Subscribe(IAsyncEnumerator<StreamMessage> internalEnumerator) {
			while (await internalEnumerator.MoveNextAsync()) {
				if (internalEnumerator.Current is not StreamMessage.Event(var resolvedEvent))
					continue;

				availableEvents.Remove(resolvedEvent.Event.EventId);

				if (availableEvents.Count == 0)
					return;
			}
		}
	}
}
