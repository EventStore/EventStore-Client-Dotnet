// ReSharper disable CheckNamespace

using System.Diagnostics;
using EventStore.Client;

namespace Shouldly;

[DebuggerStepThrough]
public static class ShouldThrowAsyncExtensions {
	public static Task<TException> ShouldThrowAsync<TException>(this EventStoreClient.ReadStreamResult source) where TException : Exception =>
		source.ToArrayAsync().AsTask().ShouldThrowAsync<TException>();
}