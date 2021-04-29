using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Grpc.Core;
namespace EventStore.Client {
	internal static class AsyncStreamReaderExtensions {
		public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncStreamReader<T> reader,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) {
			while (await reader.MoveNext(cancellationToken).ConfigureAwait(false)) {
				yield return reader.Current;
			}
		}
	}
}
