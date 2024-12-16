using System.Diagnostics;
using System.Runtime.CompilerServices;
using EventStore.Diagnostics;
using EventStore.Diagnostics.Telemetry;

namespace EventStore.Client.Diagnostics;

static class ActivityTagsCollectionExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityTagsCollection WithGrpcChannelServerTags(this ActivityTagsCollection tags, ChannelInfo? channelInfo) {
        if (channelInfo is null)
            return tags;
        
        var authorityParts = channelInfo.Channel.Target.Split(':');

        return tags
            .WithRequiredTag(TelemetryTags.Server.Address, authorityParts[0])
            .WithRequiredTag(TelemetryTags.Server.Port, int.Parse(authorityParts[1]));
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
