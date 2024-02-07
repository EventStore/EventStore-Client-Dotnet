using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Grpc.Core;

namespace EventStore.Client;

static class AsyncStreamReaderExtensions {
	public static async IAsyncEnumerable<T> ReadAllAsync<T>(
		this IAsyncStreamReader<T> reader,
		[EnumeratorCancellation]
		CancellationToken cancellationToken = default
	) {
		while (await reader.MoveNext(cancellationToken).ConfigureAwait(false))
			yield return reader.Current;
	}

	public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
#if NET
		await foreach (var item in reader.ReadAllAsync(cancellationToken))
			yield return item;
#else
		while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
			while (reader.TryRead(out T? item)) {
				yield return item;
			}
		}
#endif
	}
}
