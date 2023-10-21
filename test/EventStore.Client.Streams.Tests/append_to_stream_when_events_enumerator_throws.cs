namespace EventStore.Client {
	public class append_to_stream_when_events_enumerator_throws
		: IClassFixture<append_to_stream_when_events_enumerator_throws.Fixture> {
		private readonly Fixture _fixture;

		public append_to_stream_when_events_enumerator_throws(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public void throws_the_exception() {
			Assert.IsType<EnumerationFailedException>(_fixture.CaughtException);
		}

		[Fact]
		public async Task the_write_does_not_succeed() {
			var result = _fixture.Client.ReadStreamAsync(Direction.Forwards, _fixture.StreamName, StreamPosition.Start);
			Assert.Equal(ReadState.StreamNotFound, await result.ReadState);
		}

		private class EnumerationFailedException : Exception {
		}

		public class Fixture : EventStoreClientFixture {
			public string StreamName { get; }
			public Exception? CaughtException { get; private set; }


			public Fixture() {
				StreamName = GetStreamName("stream");
			}

			protected override async Task Given() {
				try {
					await Client.AppendToStreamAsync(StreamName, StreamRevision.None, Events());
				} catch (Exception ex) {
					CaughtException = ex;
				}

				IEnumerable<EventData> Events() {
					var i = 0;
					foreach (var e in CreateTestEvents(5)) {
						if (i++ % 3 == 0) {
							throw new EnumerationFailedException();
						}

						yield return e;
					}
				}
			}

			protected override Task When() => Task.CompletedTask;
		}
	}
}
