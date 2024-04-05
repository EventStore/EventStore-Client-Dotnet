#if !NET
namespace System.Threading.Tasks;

class TaskCompletionSource : TaskCompletionSource<object?> {
	public void SetResult()    => base.SetResult(null);
	public bool TrySetResult() => base.TrySetResult(null);
}
#endif