using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable
namespace EventStore.Client.Bugs {
	public class Issue_3320 : IAsyncLifetime {
		private readonly Fixture _fixture;

		public Issue_3320(ITestOutputHelper outputHelper) {
			_fixture = new Fixture();
			_fixture.CaptureLogs(outputHelper);
		}

		[Fact]
		public async Task receives_multiple_checkpoints_after_going_live() {
			var checkpointReached = false;
			var positions = new List<Position>();
			var tcs = new TaskCompletionSource<bool>();
			IWriteResult? writeResult = null;

			using var subscription = await _fixture.Client.SubscribeToAllAsync(Position.End, EventAppeared, false,
				SubscriptionDropped, new SubscriptionFilterOptions(StreamFilter.Prefix("a"),
					checkpointReached: CheckpointReached));


			for (var i = 0; i < 100; i++) {
				for (var prefix = 'a'; prefix <= 'z'; prefix++) {
					writeResult = await _fixture.Client.AppendToStreamAsync(
						$"{prefix}-{Guid.NewGuid():n}", StreamState.NoStream,
						_fixture.CreateTestEvents());
					positions.Add(writeResult.LogPosition);
				}
			}

			await tcs.Task.WithTimeout();
			
			Assert.True(checkpointReached);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (e.OriginalPosition!.Value >= writeResult!.LogPosition) {
					tcs.TrySetResult(true);
				}

				return Task.CompletedTask;
			}

			Task CheckpointReached(StreamSubscription s, Position position, CancellationToken ct) {
				if (position == Position.End) {
					return Task.CompletedTask;
				}

				checkpointReached = true;
				Assert.Contains(position, positions);
				if (position >= writeResult!.LogPosition) {
					tcs.TrySetResult(true);
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) {
				if (ex is null) {
					tcs.TrySetResult(true);
				} else {
					tcs.TrySetException(ex);
				}
			}
		}


		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Client.SetStreamMetadataAsync(SystemStreams.AllStream, StreamState.Any,
				new StreamMetadata(acl: new StreamAcl(SystemRoles.All)), userCredentials: TestCredentials.Root);

			protected override Task When() => Task.CompletedTask;
		}

		public Task InitializeAsync() => _fixture.InitializeAsync();

		public Task DisposeAsync() => _fixture.DisposeAsync();
	}
}
