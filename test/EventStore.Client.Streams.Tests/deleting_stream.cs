namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class deleting_stream : IClassFixture<deleting_stream.Fixture> {
		private readonly Fixture _fixture;

		public deleting_stream(Fixture fixture) {
			_fixture = fixture;
		}

		public static IEnumerable<object?[]> ExpectedStreamStateCases() {
			yield return new object?[] {StreamState.Any, nameof(StreamState.Any)};
			yield return new object?[] {StreamState.NoStream, nameof(StreamState.NoStream)};
		}

		[Theory, MemberData(nameof(ExpectedStreamStateCases))]
		public async Task hard_deleting_a_stream_that_does_not_exist_with_expected_version_does_not_throw(
			StreamState expectedVersion, string name) {
			var stream = $"{_fixture.GetStreamName()}_{name}";

			await _fixture.Client.TombstoneAsync(stream, expectedVersion);
		}

		[Regression.Fact(21, "fixed by")]
		public async Task soft_deleting_a_stream_that_exists() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(stream, StreamRevision.None, _fixture.CreateTestEvents());

			await _fixture.Client.DeleteAsync(stream, StreamState.StreamExists);
		}

		[Fact]
		public async Task hard_deleting_a_stream_that_does_not_exist_with_wrong_expected_version_throws() {
			var stream = _fixture.GetStreamName();

			await Assert.ThrowsAsync<WrongExpectedVersionException>(
				() => _fixture.Client.TombstoneAsync(stream, new StreamRevision(0)));
		}

		[Fact]
		public async Task soft_deleting_a_stream_that_does_not_exist_with_wrong_expected_version_throws() {
			var stream = _fixture.GetStreamName();

			await Assert.ThrowsAsync<WrongExpectedVersionException>(
				() => _fixture.Client.DeleteAsync(stream, new StreamRevision(0)));
		}

		[Fact]
		public async Task hard_deleting_a_stream_should_return_log_position() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents());

			var deleteResult = await _fixture.Client.TombstoneAsync(stream, writeResult.NextExpectedStreamRevision);

			Assert.True(deleteResult.LogPosition > writeResult.LogPosition);
		}

		[Fact]
		public async Task soft_deleting_a_stream_should_return_log_position() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents());

			var deleteResult = await _fixture.Client.DeleteAsync(stream, writeResult.NextExpectedStreamRevision);

			Assert.True(deleteResult.LogPosition > writeResult.LogPosition);
		}

		[Fact]
		public async Task hard_deleting_a_deleted_stream_should_throw() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);

			await Assert.ThrowsAsync<StreamDeletedException>(
				() => _fixture.Client.TombstoneAsync(stream, StreamState.NoStream));
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
