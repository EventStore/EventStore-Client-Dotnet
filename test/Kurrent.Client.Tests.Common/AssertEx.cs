using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace Kurrent.Client.Tests;

public static class AssertEx {
	/// <summary>
	/// Asserts the given function will return true before the timeout expires.
	/// Repeatedly evaluates the function until true is returned or the timeout expires.
	/// Will return immediately when the condition is true.
	/// Evaluates the timeout until expired.
	/// Will not yield the thread by default, if yielding is required to resolve deadlocks set yieldThread to true.
	/// </summary>
	/// <param name="func">The function to evaluate.</param>
	/// <param name="timeout">A timeout in milliseconds. If not specified, defaults to 1000.</param>
	/// <param name="msg">A message to display if the condition is not satisfied.</param>
	/// <param name="yieldThread">If true, the thread relinquishes the remainder of its time
	/// slice to any thread of equal priority that is ready to run.</param>
	public static async Task IsOrBecomesTrue(
		Func<Task<bool>> func, 
		TimeSpan? timeout = null,
		string msg = "AssertEx.IsOrBecomesTrue() timed out", 
		bool yieldThread = false,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	) {
		if (await IsOrBecomesTrueImpl(func, timeout, yieldThread))
			return;

		throw new XunitException($"{msg} in {memberName} {sourceFilePath}:{sourceLineNumber}");
	}

	static async Task<bool> IsOrBecomesTrueImpl(Func<Task<bool>> func, TimeSpan? timeout = null, bool yieldThread = false) {
		if (await func())
			return true;

		var expire = DateTime.UtcNow + (timeout ?? TimeSpan.FromMilliseconds(1000));
		var spin   = new SpinWait();

		while (DateTime.UtcNow <= expire) {
			if (yieldThread)
				Thread.Sleep(0);

			while (!spin.NextSpinWillYield)
				spin.SpinOnce();

			if (await func())
				return true;

			spin = new();
		}

		return false;
	}
}
