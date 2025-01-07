using System.Net;
using EventStore.Client;
using Grpc.Core;

namespace Kurrent.Client.Tests;

public class GossipChannelSelectorTests {
	[RetryFact]
	public async Task ExplicitlySettingEndPointChangesChannels() {
		var firstId  = Uuid.NewUuid();
		var secondId = Uuid.NewUuid();

		var firstSelection  = new DnsEndPoint(firstId.ToString(), 2113);
		var secondSelection = new DnsEndPoint(secondId.ToString(), 2113);

		var settings = new KurrentClientSettings {
			ConnectivitySettings = {
				DnsGossipSeeds = new[] {
					firstSelection,
					secondSelection
				},
				Insecure = true
			}
		};

		await using var channelCache = new ChannelCache(settings);
		var sut = new GossipChannelSelector(
			settings,
			channelCache,
			new FakeGossipClient(
				new(
					new ClusterMessages.MemberInfo[] {
						new(
							firstId,
							ClusterMessages.VNodeState.Leader,
							true,
							firstSelection
						),
						new(
							secondId,
							ClusterMessages.VNodeState.Follower,
							true,
							secondSelection
						)
					}
				)
			)
		);

		var channel = await sut.SelectChannelAsync(default);
		Assert.Equal($"{firstSelection.Host}:{firstSelection.Port}", channel.Target);

		channel = sut.SelectChannel(secondSelection);
		Assert.Equal($"{secondSelection.Host}:{secondSelection.Port}", channel.Target);
	}

	[RetryFact]
	public async Task ThrowsWhenDiscoveryFails() {
		var settings = new KurrentClientSettings {
			ConnectivitySettings = {
				IpGossipSeeds = new[] {
					new IPEndPoint(IPAddress.Loopback, 2113)
				},
				Insecure            = true,
				MaxDiscoverAttempts = 3
			}
		};

		await using var channelCache = new ChannelCache(settings);

		var sut = new GossipChannelSelector(settings, channelCache, new BadGossipClient());

		var ex = await Assert.ThrowsAsync<DiscoveryException>(async () => await sut.SelectChannelAsync(default));

		Assert.Equal(3, ex.MaxDiscoverAttempts);
	}

	class FakeGossipClient : IGossipClient {
		readonly ClusterMessages.ClusterInfo _clusterInfo;

		public FakeGossipClient(ClusterMessages.ClusterInfo clusterInfo) => _clusterInfo = clusterInfo;

		public ValueTask<ClusterMessages.ClusterInfo> GetAsync(
			ChannelBase channel,
			CancellationToken cancellationToken
		) =>
			new(_clusterInfo);
	}

	class BadGossipClient : IGossipClient {
		public ValueTask<ClusterMessages.ClusterInfo> GetAsync(
			ChannelBase channel,
			CancellationToken cancellationToken
		) =>
			throw new NotSupportedException();
	}
}
