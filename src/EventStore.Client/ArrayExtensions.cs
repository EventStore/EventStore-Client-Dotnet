namespace EventStore.Client;

static class ArrayExtensions {
	public static void RandomShuffle<T>(this T[] arr, int i, int j) {
		if (i >= j)
			return;

		var rnd = new Random(Guid.NewGuid().GetHashCode());
		for (var k = i; k < j; ++k) {
			var index = rnd.Next(k, j + 1);
			var tmp   = arr[index];
			arr[index] = arr[k];
			arr[k]     = tmp;
		}
	}
}