using System.Diagnostics;

namespace EventStore.Client;

public static class TaskExtensions {
	public static Task WithTimeout(this Task task, TimeSpan timeout)
		=> task.WithTimeout(Convert.ToInt32(timeout.TotalMilliseconds));

	public static async Task WithTimeout(this Task task, int timeoutMs = 15000, string? message = null) {
		if (Debugger.IsAttached) timeoutMs = -1;

		if (await Task.WhenAny(task, Task.Delay(timeoutMs)) != task)
			throw new TimeoutException(message ?? "Timed out waiting for task");

		await task;
	}

	public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
		=> task.WithTimeout(Convert.ToInt32(timeout.TotalMilliseconds));

	public static async Task<T> WithTimeout<T>(this Task<T> task, int timeoutMs = 15000, string? message = null) {
		if (Debugger.IsAttached) timeoutMs = -1;

		if (await Task.WhenAny(task, Task.Delay(timeoutMs)) == task)
			return await task;

		throw new TimeoutException(message ?? "Timed out waiting for task");
	}
}