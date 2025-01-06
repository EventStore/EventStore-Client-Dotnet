using System.Diagnostics;
using System.Runtime.CompilerServices;
using EventStore.Diagnostics;
using EventStore.Diagnostics.Telemetry;

namespace EventStore.Client.Diagnostics;

static class ActivityTagsCollectionExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityTagsCollection WithGrpcChannelServerTags(this ActivityTagsCollection tags, EventStoreClientSettings settings, ChannelInfo? channelInfo) {
        if (channelInfo is null)
            return tags;

		var authorityParts = channelInfo.Channel.Target.Split([':'], StringSplitOptions.RemoveEmptyEntries);

		string host = authorityParts[0];
		int port = authorityParts.Length == 1
			? settings.ConnectivitySettings.Insecure ? 80 : 443
			: int.Parse(authorityParts[1]);

		return tags
			.WithRequiredTag(TelemetryTags.Server.Address, host)
			.WithRequiredTag(TelemetryTags.Server.Port, port);
	}
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityTagsCollection WithClientSettingsServerTags(this ActivityTagsCollection source, EventStoreClientSettings settings) {
        if (settings.ConnectivitySettings.DnsGossipSeeds?.Length != 1)
            return source;
        
        var gossipSeed = settings.ConnectivitySettings.DnsGossipSeeds[0];

        return source
            .WithRequiredTag(TelemetryTags.Server.Address, gossipSeed.Host)
            .WithRequiredTag(TelemetryTags.Server.Port, gossipSeed.Port);
    }
}
