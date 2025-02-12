using EventStore.Client;
using Grpc.Core;
using Humanizer;

namespace Kurrent.Client.Tests.Streams;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class AppendTestsWithAutoSerialization(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[Theory, ExpectedVersionCreateStreamTestCases]
	public async Task appending_zero_events(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		const int iterations = 2;
		for (var i = 0; i < iterations; i++) {
			var writeResult = await Fixture.Streams.AppendToStreamAsync(
				stream,
				[],
				new AppendToStreamOptions { ExpectedStreamState = expectedStreamState }
			);

			writeResult.NextExpectedStreamRevision.ShouldBe(StreamRevision.None);
		}

		await Fixture.Streams
			.ReadStreamAsync(stream, new ReadStreamOptions { MaxCount = iterations })
			.ShouldThrowAsync<StreamNotFoundException>(ex => ex.Stream.ShouldBe(stream));
	}

	[Theory, ExpectedVersionCreateStreamTestCases]
	public async Task appending_zero_events_again(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		const int iterations = 2;
		for (var i = 0; i < iterations; i++) {
			var writeResult = await Fixture.Streams.AppendToStreamAsync(
				stream,
				[],
				new AppendToStreamOptions { ExpectedStreamState = expectedStreamState }
			);

			Assert.Equal(StreamRevision.None, writeResult.NextExpectedStreamRevision);
		}

		await Fixture.Streams
			.ReadStreamAsync(stream, new ReadStreamOptions { MaxCount = iterations })
			.ShouldThrowAsync<StreamNotFoundException>(ex => ex.Stream.ShouldBe(stream));
	}

	[Theory, ExpectedVersionCreateStreamTestCases]
	public async Task create_stream_expected_version_on_first_write_if_does_not_exist(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions { ExpectedStreamState = expectedStreamState }
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var count = await Fixture.Streams.ReadStreamAsync(stream, new ReadStreamOptions { MaxCount = 2 })
			.CountAsync();

		Assert.Equal(1, count);
	}

	[RetryFact]
	public async Task multiple_idempotent_writes() {
		var stream   = Fixture.GetStreamName();
		var messages = Fixture.CreateTestMessages(4).ToArray();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messages);
		Assert.Equal(new(3), writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messages);
		Assert.Equal(new(3), writeResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task multiple_idempotent_writes_with_same_id_bug_case() {
		var stream = Fixture.GetStreamName();

		var message  = Fixture.CreateTestMessages().First();
		var messages = new[] { message, message, message, message, message, message };

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messages);

		Assert.Equal(new(5), writeResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task
		in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_any_then_next_expected_version_is_unreliable() {
		var stream = Fixture.GetStreamName();

		var message  = Fixture.CreateTestMessages().First();
		var messages = new[] { message, message, message, message, message, message };

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messages);

		Assert.Equal(new(5), writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, messages);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task
		in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_nostream_then_next_expected_version_is_correct() {
		var stream = Fixture.GetStreamName();

		var message        = Fixture.CreateTestMessages().First();
		var messages       = new[] { message, message, message, message, message, message };
		var streamRevision = StreamRevision.FromInt64(messages.Length - 1);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			messages,
			new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream }
		);

		Assert.Equal(streamRevision, writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			messages,
			new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream }
		);

		Assert.Equal(streamRevision, writeResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task writing_with_correct_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(
				stream,
				Fixture.CreateTestMessages(1),
				new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream }
			)
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task returns_log_position_when_writing() {
		var stream = Fixture.GetStreamName();

		var result = await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages(1));

		Assert.True(0 < result.LogPosition.PreparePosition);
		Assert.True(0 < result.LogPosition.CommitPosition);
	}

	[RetryFact]
	public async Task writing_with_any_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(
				stream,
				Fixture.CreateTestMessages(1),
				new AppendToStreamOptions { ExpectedStreamState = StreamState.Any }
			)
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task writing_with_invalid_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(
				stream,
				Fixture.CreateTestMessages(),
				new AppendToStreamOptions { ExpectedStreamRevision = new StreamRevision(5) }
			)
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task append_with_correct_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages(1));

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions { ExpectedStreamRevision = writeResult.NextExpectedStreamRevision }
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task append_with_any_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages(1));

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(1),
			new AppendToStreamOptions { ExpectedStreamState = StreamState.Any }
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task appending_with_wrong_expected_version_to_existing_stream_throws_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages());

		var ex = await Fixture.Streams
			.AppendToStreamAsync(
				stream,
				Fixture.CreateTestMessages(),
				new AppendToStreamOptions { ExpectedStreamRevision = new StreamRevision(999) }
			)
			.ShouldThrowAsync<WrongExpectedVersionException>();

		ex.ActualStreamRevision.ShouldBe(new(0));
		ex.ExpectedStreamRevision.ShouldBe(new(999));
	}

	[RetryFact]
	public async Task appending_with_wrong_expected_version_to_existing_stream_returns_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamRevision    = new StreamRevision(1),
				ConfigureOperationOptions = options => { options.ThrowOnAppendFailure = false; }
			}
		);

		var wrongExpectedVersionResult = (WrongExpectedVersionResult)writeResult;

		Assert.Equal(new(1), wrongExpectedVersionResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task append_with_stream_exists_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamState = StreamState.NoStream
			}
		);

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamState = StreamState.StreamExists
			}
		);
	}

	[RetryFact]
	public async Task append_with_stream_exists_expected_version_to_stream_with_multiple_events() {
		var stream = Fixture.GetStreamName();

		for (var i = 0; i < 5; i++)
			await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages(1));

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamState = StreamState.StreamExists
			}
		);
	}

	[RetryFact]
	public async Task append_with_stream_exists_expected_version_if_metadata_stream_exists() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(
			stream,
			StreamState.Any,
			new(10, default)
		);

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamState = StreamState.StreamExists
			}
		);
	}

	[RetryFact]
	public async Task
		appending_with_stream_exists_expected_version_and_stream_does_not_exist_throws_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var ex = await Fixture.Streams
			.AppendToStreamAsync(
				stream,
				Fixture.CreateTestMessages(),
				new AppendToStreamOptions {
					ExpectedStreamState = StreamState.StreamExists
				}
			)
			.ShouldThrowAsync<WrongExpectedVersionException>();

		ex.ActualStreamRevision.ShouldBe(StreamRevision.None);
	}

	[RetryFact]
	public async Task
		appending_with_stream_exists_expected_version_and_stream_does_not_exist_returns_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamState       = StreamState.StreamExists,
				ConfigureOperationOptions = options => { options.ThrowOnAppendFailure = false; }
			}
		);

		var wrongExpectedVersionResult = Assert.IsType<WrongExpectedVersionResult>(writeResult);

		Assert.Equal(StreamRevision.None, wrongExpectedVersionResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task appending_with_stream_exists_expected_version_to_hard_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(
				stream,
				Fixture.CreateTestMessages(),
				new AppendToStreamOptions {
					ExpectedStreamState = StreamState.StreamExists
				}
			)
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task appending_with_stream_exists_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages());

		await Fixture.Streams.DeleteAsync(stream, StreamState.Any);

		await Fixture.Streams
			.AppendToStreamAsync(
				stream,
				Fixture.CreateTestMessages(),
				new AppendToStreamOptions {
					ExpectedStreamState = StreamState.StreamExists
				}
			)
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task can_append_multiple_events_at_once() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages(100));

		Assert.Equal(new(99), writeResult.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task expected_version_no_stream() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamState = StreamState.NoStream
			}
		);

		Assert.Equal(new(0), result!.NextExpectedStreamRevision);
	}

	[RetryFact]
	public async Task expected_version_no_stream_returns_position() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			Fixture.CreateTestMessages(),
			new AppendToStreamOptions {
				ExpectedStreamState = StreamState.NoStream
			}
		);

		Assert.True(result.LogPosition > Position.Start);
	}

	[RetryFact]
	public async Task with_timeout_any_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var ex = await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(100),
			new AppendToStreamOptions {
				ExpectedStreamState = StreamState.Any,
				Deadline            = TimeSpan.FromTicks(1)
			}
		).ShouldThrowAsync<RpcException>();

		ex.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);
	}

	[RetryFact]
	public async Task with_timeout_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, Fixture.CreateTestMessages());

		var ex = await Fixture.Streams.AppendToStreamAsync(
			stream,
			Fixture.CreateTestMessages(10),
			new AppendToStreamOptions {
				ExpectedStreamRevision = new StreamRevision(0),
				Deadline               = TimeSpan.Zero
			}
		).ShouldThrowAsync<RpcException>();

		ex.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);
	}

	// [RetryFact]
	// public async Task when_events_enumerator_throws_the_write_does_not_succeed() {
	// 	var streamName = Fixture.GetStreamName();
	//
	// 	await Fixture.Streams
	// 		.AppendToStreamAsync(
	// 			streamName,
	// 			StreamRevision.None,
	// 			Fixture.CreateTestMessagesThatThrowsException(),
	// 			userCredentials: new UserCredentials(TestCredentials.Root.Username!, TestCredentials.Root.Password!)
	// 		)
	// 		.ShouldThrowAsync<Exception>();
	//
	// 	var state = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start)
	// 		.ReadState;
	//
	// 	state.ShouldBe(ReadState.StreamNotFound);
	// }
	//
	// [RetryFact]
	// public async Task succeeds_when_size_is_less_than_max_append_size() {
	// 	// Arrange
	// 	var maxAppendSize = (uint)100.Kilobytes().Bytes;
	// 	var stream        = Fixture.GetStreamName();
	//
	// 	// Act
	// 	var (messages, size) = Fixture.CreateTestMessagesUpToMaxSize(maxAppendSize - 1);
	//
	// 	// Assert
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// }
	//
	// [RetryFact]
	// public async Task fails_when_size_exceeds_max_append_size() {
	// 	// Arrange
	// 	var maxAppendSize    = (uint)100.Kilobytes().Bytes;
	// 	var stream           = Fixture.GetStreamName();
	// 	var eventsAppendSize = maxAppendSize * 2;
	//
	// 	// Act
	// 	var (messages, size) = Fixture.CreateTestMessagesUpToMaxSize(eventsAppendSize);
	//
	// 	// Assert
	// 	size.ShouldBeGreaterThan(maxAppendSize);
	//
	// 	var ex = await Fixture.Streams
	// 		.AppendToStreamAsync(stream, StreamState.NoStream, messages)
	// 		.ShouldThrowAsync<MaximumAppendSizeExceededException>();
	//
	// 	ex.MaxAppendSize.ShouldBe(maxAppendSize);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0em1_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(6).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_3e2_4e3_4e4_0any_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(6).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e5_non_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(6).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(5), messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 2).CountAsync();
	//
	// 	Assert.Equal(messages.Length + 1, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e6_throws_wev() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(6).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	//
	// 	await Assert.ThrowsAsync<WrongExpectedVersionException>(
	// 		() => Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(6), messages.Take(1))
	// 	);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e6_returns_wev() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(6).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	//
	// 	var writeResult = await Fixture.Streams.AppendToStreamAsync(
	// 		stream,
	// 		new StreamRevision(6),
	// 		messages.Take(1),
	// 		options => options.ThrowOnAppendFailure = false
	// 	);
	//
	// 	Assert.IsType<WrongExpectedVersionResult>(writeResult);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e4_throws_wev() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(6).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	//
	// 	await Assert.ThrowsAsync<WrongExpectedVersionException>(
	// 		() => Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(4), messages.Take(1))
	// 	);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e4_returns_wev() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(6).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	//
	// 	var writeResult = await Fixture.Streams.AppendToStreamAsync(
	// 		stream,
	// 		new StreamRevision(4),
	// 		messages.Take(1),
	// 		options => options.ThrowOnAppendFailure = false
	// 	);
	//
	// 	Assert.IsType<WrongExpectedVersionResult>(writeResult);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_0e0_non_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages().ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(0), messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 2).CountAsync();
	//
	// 	Assert.Equal(messages.Length + 1, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_0any_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages().ToArray();
	//
	// 	await Task.Delay(TimeSpan.FromSeconds(30));
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_0em1_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages().ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_0em1_1e0_2e1_1any_1any_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(3).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, messages.Skip(1).Take(1));
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, messages.Skip(1).Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_S_0em1_1em1_E_S_0em1_E_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(2).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_S_0em1_1em1_E_S_0any_E_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(2).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, messages.Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_S_0em1_1em1_E_S_1e0_E_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(2).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(0), messages.Skip(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_S_0em1_1em1_E_S_1any_E_idempotent() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(2).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages);
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, messages.Skip(1).Take(1));
	//
	// 	var count = await Fixture.Streams
	// 		.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, messages.Length + 1).CountAsync();
	//
	// 	Assert.Equal(messages.Length, count);
	// }
	//
	// [RetryFact]
	// public async Task sequence_S_0em1_1em1_E_S_0em1_1em1_2em1_E_idempotancy_fail_throws() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(3).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages.Take(2));
	//
	// 	await Assert.ThrowsAsync<WrongExpectedVersionException>(
	// 		() => Fixture.Streams.AppendToStreamAsync(
	// 			stream,
	// 			StreamState.NoStream,
	// 			messages
	// 		)
	// 	);
	// }
	//
	// [RetryFact]
	// public async Task sequence_S_0em1_1em1_E_S_0em1_1em1_2em1_E_idempotancy_fail_returns() {
	// 	var stream = Fixture.GetStreamName();
	//
	// 	var messages = Fixture.CreateTestMessages(3).ToArray();
	//
	// 	await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messages.Take(2));
	//
	// 	var writeResult = await Fixture.Streams.AppendToStreamAsync(
	// 		stream,
	// 		StreamState.NoStream,
	// 		messages,
	// 		options => options.ThrowOnAppendFailure = false
	// 	);
	//
	// 	Assert.IsType<WrongExpectedVersionResult>(writeResult);
	// }

	// [Fact]
	// public async Task sending_and_receiving_large_messages_over_the_hard_limit() {
	// 	uint maxAppendSize = 16 * 1024 * 1024 - 10000;
	// 	var  streamName    = Fixture.GetStreamName();
	// 	var largeEvent = Fixture.CreateTestMessages()
	// 		.Select(e => new EventData(e.EventId, "-", new byte[maxAppendSize + 1]));
	//
	// 	var ex = await Assert.ThrowsAsync<RpcException>(() => Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, largeEvent));
	//
	// 	Assert.Equal(StatusCode.ResourceExhausted, ex.StatusCode);
	// }

	class ExpectedVersionCreateStreamTestCases : TestCaseGenerator<ExpectedVersionCreateStreamTestCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [StreamState.Any];
			yield return [StreamState.NoStream];
		}
	}
}
