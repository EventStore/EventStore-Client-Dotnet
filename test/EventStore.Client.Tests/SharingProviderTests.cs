using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
namespace EventStore.Client {
	public class SharingProviderTests {
		[Fact]
		public async Task CanGetCurrent() {
			var sut = new SharingProvider<int, int>(
				factory: async (x, _) => x + 1,
				initialInput: 5);
			Assert.Equal(6, await sut.CurrentAsync);
		}

		[Fact]
		public async Task CanReset() {
			var count = 0;
			var sut = new SharingProvider<bool, int>(
				factory: async (_, _) => count++,
				initialInput: true);

			Assert.Equal(0, await sut.CurrentAsync);
			sut.Reset();
			Assert.Equal(1, await sut.CurrentAsync);
		}

		[Fact]
		public async Task CanReturnBroken() {
			Action<bool> onBroken = null;
			var count = 0;
			var sut = new SharingProvider<bool, int>(
				factory: async (_, f) => {
					onBroken = f;
					return count++;
				},
				initialInput: true);

			Assert.Equal(0, await sut.CurrentAsync);

			onBroken(true);
			Assert.Equal(1, await sut.CurrentAsync);

			onBroken(true);
			Assert.Equal(2, await sut.CurrentAsync);
		}

		[Fact]
		public async Task CanReturnSameBoxTwice() {
			Action<bool> onBroken = null;
			var count = 0;
			var sut = new SharingProvider<bool, int>(
				factory: async (_, f) => {
					onBroken = f;
					return count++;
				},
				initialInput: true);

			Assert.Equal(0, await sut.CurrentAsync);

			var firstOnBroken = onBroken;
			firstOnBroken(true);
			firstOnBroken(true);
			firstOnBroken(true);

			// factory is only executed once
			Assert.Equal(1, await sut.CurrentAsync);
		}

		[Fact]
		public async Task CanReturnPendingBox() {
			var trigger = new SemaphoreSlim(0);
			Action<bool> onBroken = null;
			var count = 0;
			var sut = new SharingProvider<bool, int>(
				factory: async (_, f) => {
					onBroken = f;
					count++;
					await trigger.WaitAsync();
					return count;
				},
				initialInput: true);


			var currentTask = sut.CurrentAsync;

			Assert.False(currentTask.IsCompleted);

			// return it even though it is pending
			onBroken(true);

			// box wasn't replaced
			Assert.Equal(currentTask, sut.CurrentAsync);

			// factory was not called again
			Assert.Equal(1, count);

			// complete whatever factory calls
			trigger.Release(100);

			// can get the value now
			Assert.Equal(1, await sut.CurrentAsync);

			// factory still wasn't called again
			Assert.Equal(1, count);
		}

		[Fact]
		public async Task FactoryCanThrow() {
			var sut = new SharingProvider<int, int>(
				factory: (x, _) => throw new Exception($"input {x}"),
				initialInput: 0);

			// exception propagated to consumer
			var ex = await Assert.ThrowsAsync<Exception>(async () => {
				await sut.CurrentAsync;
			});

			Assert.Equal("input 0", ex.Message);
		}

		[Fact]
		public async Task ExampleUsage() {
			// factory waits to be signalled.
			// sometimes the factory succeeds, sometimes it throws.
			var completeConstruction = new SemaphoreSlim(0);
			var triggerFailure = new SemaphoreSlim(0);
			var failed = new SemaphoreSlim(0);

			async Task<int> Factory(int input, Action<int> onBroken) {
				await completeConstruction.WaitAsync();
				if (input == 2) {
					throw new Exception($"fail to create {input} in factory");
				} else {
					_ = triggerFailure.WaitAsync().ContinueWith(t => {
						onBroken(input + 1);
						failed.Release();
					});
					return input;
				}
			}

			var sut = new SharingProvider<int, int>(Factory, 0);

			// got a value
			completeConstruction.Release();
			Assert.Equal(0, await sut.CurrentAsync);

			// break item fails
			triggerFailure.Release();
			await failed.WaitAsync();

			// got a new value
			completeConstruction.Release();
			Assert.Equal(1, await sut.CurrentAsync);

			// item fails, then fail to create a new item
			triggerFailure.Release();
			await failed.WaitAsync();

			var t = sut.CurrentAsync;
			completeConstruction.Release();
			var ex = await Assert.ThrowsAsync<Exception>(async () => {
				await t;
			});
			Assert.Equal("fail to create 2 in factory", ex.Message);

			// got a new value, back to initial
			completeConstruction.Release();
			Assert.Equal(0, await sut.CurrentAsync);
		}
	}
}
