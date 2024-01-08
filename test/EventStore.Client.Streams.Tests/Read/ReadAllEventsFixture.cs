namespace EventStore.Client.Streams.Tests.Read;

public class ReadAllEventsFixture : EventStoreFixture {
	public ReadAllEventsFixture() {
		OnSetup = async () => {
			_ = await Streams.SetStreamMetadataAsync(
				SystemStreams.AllStream,
				StreamState.NoStream,
				new(acl: new(SystemRoles.All)),
				userCredentials: TestCredentials.Root
			);
			
			Events = Enumerable
				.Concat(
					CreateTestEvents(20),
					CreateTestEvents(2, metadataSize: 1_000_000)
				)
				.ToArray();

			ExpectedStreamName = GetStreamName();
			
			await Streams.AppendToStreamAsync(ExpectedStreamName, StreamState.NoStream, Events);

			ExpectedEvents         = Events.ToBinaryData();
			ExpectedEventsReversed = ExpectedEvents.Reverse().ToArray();

			ExpectedFirstEvent = ExpectedEvents.First();
			ExpectedLastEvent  = ExpectedEvents.Last();
		};
	}

	public string ExpectedStreamName { get; private set; } = null!;
	
	public EventData[] Events { get; private set; } = Array.Empty<EventData>();
	
	public EventBinaryData[] ExpectedEvents         { get; private set; } = Array.Empty<EventBinaryData>();
	public EventBinaryData[] ExpectedEventsReversed { get; private set; } = Array.Empty<EventBinaryData>();

	public EventBinaryData ExpectedFirstEvent { get; private set; }
	public EventBinaryData ExpectedLastEvent  { get; private set; }
}