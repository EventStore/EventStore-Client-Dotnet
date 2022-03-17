using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	// Given a factory for items of type TOutput, where the items:
	//  - are expensive to produce
	//  - can be shared by consumers
	//  - can break
	//  - can fail to be successfully produced by the factory to begin with.
	//
	// This class will make minimal use of the factory to provide items to consumers.
	// The Factory can produce and return an item, or it can throw an exception.
	// We pass the factory a OnBroken callback to be called later if that instance becomes broken.
	//   the OnBroken callback can be called multiple times, the factory will be called once.
	//   the argument to the OnBroken callback is the input to construct the next item.
	//
	// The factory will not be called multiple times concurrently so does not need to be
	// thread safe, but it does need to terminate.
	//
	// This class is thread safe.
	internal class SharingProvider<TInput, TOutput> {
		private readonly Func<TInput, Action<TInput>, Task<TOutput>> _factory;
		private readonly TInput _initialInput;
		private TaskCompletionSource<TOutput> _currentBox;

		public SharingProvider(Func<TInput, Action<TInput>, Task<TOutput>> factory, TInput initialInput) {
			_factory = factory;
			_initialInput = initialInput;
			_currentBox = new(TaskCreationOptions.RunContinuationsAsynchronously);
			_ = FillBoxAsync(_currentBox, input: initialInput);
		}

		public Task<TOutput> CurrentAsync => _currentBox.Task;

		public void Reset() {
			OnBroken(_currentBox, _initialInput);
		}

		// Call this to return a box containing a defective item, or indeed no item at all.
		// A new box will be produced and filled if necessary.
		private void OnBroken(TaskCompletionSource<TOutput> brokenBox, TInput input) {
			if (!brokenBox.Task.IsCompleted) {
				// factory is still working on this box. don't create a new box to fill
				// or we would have to require the factory be thread safe.
				return;
			}

			// replace _currentBox with a new one, but only if it is the broken one.
			var originalBox = Interlocked.CompareExchange(
				location1: ref _currentBox,
				value: new(TaskCreationOptions.RunContinuationsAsynchronously),
				comparand: brokenBox);

			if (originalBox == brokenBox) {
				// replaced the _currentBox, call the factory to fill it.
				_ = FillBoxAsync(_currentBox, input);
			} else {
				// did not replace. a new one was created previously. do nothing.
			}
		}

		private async Task FillBoxAsync(TaskCompletionSource<TOutput> box, TInput input) {
			try {
				var item = await _factory(input, x => OnBroken(box, x)).ConfigureAwait(false);
				box.TrySetResult(item);
			} catch (Exception ex) {
				await Task.Yield(); // avoid risk of stack overflow
				box.TrySetException(ex);
				OnBroken(box, _initialInput);
			}
		}
	}
}
