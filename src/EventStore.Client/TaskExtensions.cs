using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal static class TaskExtensions {
		// To give up waiting for the task, cancel the token.
		// obvs this wouldn't cancel the task itself.
		public static async ValueTask<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {
			if (task.Status == TaskStatus.RanToCompletion)
				return task.Result;

			await Task
				.WhenAny(
					task,
					Task.Delay(-1, cancellationToken))
				.ConfigureAwait(false);

			cancellationToken.ThrowIfCancellationRequested();
			return await task.ConfigureAwait(false);
		}
	}
}
