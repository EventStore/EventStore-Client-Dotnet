using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventStore.Client {
	internal class SharingProvider {
		protected ILogger Log { get; }

		public SharingProvider(ILoggerFactory? loggerFactory) {
			Log = loggerFactory?.CreateLogger<SharingProvider>() ??
					new NullLogger<SharingProvider>();
		}
	}

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


	internal class SharingProvider<TInput, TOutput> : SharingProvider, IDisposable {
		private readonly Func<TInput, Action<TInput>, Task<TOutput>>
			_factory;

		private readonly TimeSpan                      _factoryRetryDelay;
		private          TInput                        _previousInput;
		private          TaskCompletionSource<TOutput> _currentBox;
		private          bool                          _disposed;
		private readonly SemaphoreSlim                 _syncLock = new SemaphoreSlim(1, 1);

		public SharingProvider(
			Func<TInput, Action<TInput>, Task<TOutput>> factory,
			TimeSpan factoryRetryDelay,
			TInput previousInput,
			ILoggerFactory? loggerFactory = null) : base(loggerFactory) {

			_factory = factory;
			_factoryRetryDelay = factoryRetryDelay;
			_previousInput = previousInput;
			_currentBox = new(TaskCreationOptions.RunContinuationsAsynchronously);
			_ = FillBoxAsync(_currentBox, input: previousInput);
		}

		public Task<TOutput> CurrentAsync => _currentBox.Task;

		public void Reset() {
			OnBroken(_currentBox, _previousInput);
		}

		// Call this to return a box containing a defective item, or indeed no item at all.
		// A new box will be produced and filled if necessary.
		private void OnBroken(TaskCompletionSource<TOutput> brokenBox, TInput input) {
			if (!brokenBox.Task.IsCompleted) {
				// factory is still working on this box. don't create a new box to fill
				// or we would have to require the factory be thread safe.
				Log.LogDebug("{type} returned to factory. Production already in progress.", typeof(TOutput).Name);
				return;
			}

			// replace _currentBox with a new one, but only if it is the broken one.
			var originalBox = Interlocked.CompareExchange(
				location1: ref _currentBox,
				value: new(TaskCreationOptions.RunContinuationsAsynchronously),
				comparand: brokenBox);

			if (originalBox == brokenBox) {
				// replaced the _currentBox, call the factory to fill it.
				Log.LogDebug("{type} returned to factory. Producing a new one.", typeof(TOutput).Name);
				_ = FillBoxAsync(_currentBox, input);
			} else {
				// did not replace. a new one was created previously. do nothing.
				Log.LogDebug("{type} returned to factory. Production already complete.", typeof(TOutput).Name);
			}
		}

		private async Task FillBoxAsync(TaskCompletionSource<TOutput> box, TInput input) {
			if (_disposed) {
				Log.LogDebug("{type} will not be produced, factory is closed!", typeof(TOutput).Name);
				box.TrySetException(new ObjectDisposedException(GetType().ToString()));
				return;
			}

			try {
				Log.LogDebug("{type} being produced...", typeof(TOutput).Name);
				var item = await _factory(input, x => OnBroken(box, x)).ConfigureAwait(false);
				box.TrySetResult(item);
				Log.LogDebug("{type} produced!", typeof(TOutput).Name);
			} catch (Exception ex) {
				await Task.Yield(); // avoid risk of stack overflow
				Log.LogDebug(ex, "{type} production failed. Retrying in {delay}", typeof(TOutput).Name, _factoryRetryDelay);
				await Task.Delay(_factoryRetryDelay).ConfigureAwait(false);
				box.TrySetException(ex);
				OnBroken(box, _previousInput);
			}
		}

		public async Task<TOutput> GetAsync(TInput input) {
			await _syncLock.WaitAsync().ConfigureAwait(false);
			try {
				if (Equals(input, _previousInput)) {
					return await CurrentAsync.ConfigureAwait(false);
				}

				if (_currentBox.Task.IsCompleted && Equals(input, _previousInput))
					return await CurrentAsync.ConfigureAwait(false);

				_previousInput = input;
				var newBox      = new TaskCompletionSource<TOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
				var originalBox = Interlocked.Exchange(ref _currentBox, newBox);
				if (originalBox != newBox) {
					_ = FillBoxAsync(newBox, input);
				}

				return await CurrentAsync.ConfigureAwait(false);
			} finally {
				_syncLock.Release();
			}
		}

		public void Dispose() {
			_disposed = true;
		}
	}
}
