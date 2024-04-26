// ReSharper disable CheckNamespace

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EventStore.Diagnostics;

static class ActivityTagsCollectionExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityTagsCollection WithRequiredTag(this ActivityTagsCollection source, string key, object? value) {
        source[key] = value ?? throw new ArgumentNullException(key);
        return source;
    }
    
    /// <summary>
    /// - If the key previously existed in the collection and the value is <see langword="null" />, the collection item matching the key will get removed from the collection.
    /// - If the key previously existed in the collection and the value is not <see langword="null" />, the value will replace the old value stored in the collection.
    /// - Otherwise, a new item will get added to the collection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityTagsCollection WithOptionalTag(this ActivityTagsCollection source, string key, object? value) {
        source[key] = value;
        return source;
    }
}
