using System.Collections.Generic;
#nullable enable
namespace EventStore.Client {
#if NETFRAMEWORK
	internal static class DeconstructionExtensions {
		public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey key,
			out TValue value) {
			key = source.Key;
			value = source.Value;
		}
	}
#endif
}
