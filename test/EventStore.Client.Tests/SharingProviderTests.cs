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
			Action<bool>? onBroken = null;
			var count = 0;
			var sut = new SharingProvider<bool, int>(
				factory: async (_, f) => {
					onBroken = f;
					return count++;
				},
				initialInput: true);

			Assert.Equal(0, await sut.CurrentAsync);

			onBroken?.Invoke(true);
			Assert.Equal(1, await sut.CurrentAsync);

			onBroken?.Invoke(true);
			Assert.Equal(2, await sut.CurrentAsync);
		}

		[Fact]
		public async Task CanReturnSameBoxTwice() {
			Action<bool>? onBroken = null;
			var count = 0;
			var sut = new SharingProvider<bool, int>(
				factory: async (_, f) => {
					onBroken = f;
					return count++;
				},
				initialInput: true);

			Assert.Equal(0, await sut.CurrentAsync);

			var firstOnBroken = onBroken;
			firstOnBroken?.Invoke(true);
			firstOnBroken?.Invoke(true);
			firstOnBroken?.Invoke(true);

			// factory is only executed once
			Assert.Equal(1, await sut.CurrentAsync);
		}

		[Fact]
		public async Task CanReturnPendingBox() {
			var trigger = new SemaphoreSlim(0);
			Action<bool>? onBroken = null;
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
			onBroken?.Invoke(true);

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

		// safe to call onBroken before the factory has returned, but it doesn't
		// do anything because the box is not populated yet.
		// the factory has to indicate failure by throwing.
		[Fact]
		public async Task FactoryCanCallOnBrokenSynchronously() {
			var sut = new SharingProvider<int, int>(
				factory: async (x, onBroken) => {
					if (x == 0)
						onBroken(5);
					return x;
				},
				initialInput: 0);

			// onBroken was called but it didn't do anything
			Assert.Equal(0, await sut.CurrentAsync);
		}

		[Fact]
		public async Task FactoryCanCallOnBrokenSynchronouslyAndThrow() {
			var sut = new SharingProvider<int, int>(
				factory: async (x, onBroken) => {
					if (x == 0) {
						onBroken(5);
						throw new Exception($"input {x}");
					}
					return x;
				},
				initialInput: 0);

			var ex = await Assert.ThrowsAsync<Exception>(async () => {
				await sut.CurrentAsync;
			});

			Assert.Equal("input 0", ex.Message);
		}

		[Fact]
		public async Task ExampleUsage() {
			// factory waits to be signalled by completeConstruction being released
			// sometimes the factory succeeds, sometimes it throws.
			// failure of the produced item is trigged by 
			var completeConstruction = new SemaphoreSlim(0);
			var constructionCompleted = new SemaphoreSlim(0);

			var triggerFailure = new SemaphoreSlim(0);
			var failed = new SemaphoreSlim(0);

			async Task<int> Factory(int input, Action<int> onBroken) {
				await completeConstruction.WaitAsync();
				try {
					if (input == 2) {
						throw new Exception($"fail to create {input} in factory");
					} else {
						_ = triggerFailure.WaitAsync().ContinueWith(t => {
							onBroken(input + 1);
							failed.Release();
						});
						return input;
					}
				} finally {
					constructionCompleted.Release();
				}
			}

			var sut = new SharingProvider<int, int>(Factory, 0);

			// got an item (0)
			completeConstruction.Release();
			Assert.Equal(0, await sut.CurrentAsync);

			// when item 0 fails
			triggerFailure.Release();
			await failed.WaitAsync();

			// then a new item is produced (1)
			await constructionCompleted.WaitAsync();
			completeConstruction.Release();
			Assert.Equal(1, await sut.CurrentAsync);

			// when item 1 fails
			triggerFailure.Release();
			await failed.WaitAsync();

			// then item 2 is not created
			var t = sut.CurrentAsync;
			await constructionCompleted.WaitAsync();
			completeConstruction.Release();
			var ex = await Assert.ThrowsAsync<Exception>(async () => {
				await t;
			});
			Assert.Equal("fail to create 2 in factory", ex.Message);

			// when the factory is allowed to produce another item (0), it does:
			await constructionCompleted.WaitAsync();
			completeConstruction.Release();
			// the previous box failed to be constructured, the factory will be called to produce another
			// one. but until this has happened the old box with the error is the current one.
			// therefore wait until the factory has had a chance to attempt another construction.
			// the previous awaiting this semaphor are only there so that we can tell when
			// this one is done.
			await constructionCompleted.WaitAsync();
			Assert.Equal(0, await sut.CurrentAsync);
		}
	}
}
