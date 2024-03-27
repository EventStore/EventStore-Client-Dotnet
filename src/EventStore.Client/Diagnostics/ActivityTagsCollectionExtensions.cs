#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Runtime;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using EventStore.Client.Diagnostics.OpenTelemetry;

namespace EventStore.Client.Diagnostics;

public static class ActivityTagsCollectionExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityTagsCollection WithTagsFrom(
		this ActivityTagsCollection tags, EventStoreClientSettings settings
	) {
		return tags.WithTags(
			new() {
				{ SemanticAttributes.ServerAddress, settings.ConnectivitySettings.ResolvedAddressOrDefault.Host },
				{ SemanticAttributes.ServerPort, settings.ConnectivitySettings.ResolvedAddressOrDefault.Port }
			}
		).WithTagsFrom(settings.DefaultCredentials);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityTagsCollection WithTagsFrom(
		this ActivityTagsCollection tags, UserCredentials? userCredentials
	) {
		return tags.WithTag(SemanticAttributes.DatabaseUser, userCredentials?.Username);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ActivityTagsCollection WithTags(this ActivityTagsCollection current, ActivityTagsCollection? tags)
		=> tags == null
			? current
			: tags.Aggregate(current, (newTags, tag) => newTags.WithTag(tag.Key, tag.Value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ActivityTagsCollection WithTag(this ActivityTagsCollection tags, string key, object? value) {
		tags[key] = value;
		return tags;
	}
}
