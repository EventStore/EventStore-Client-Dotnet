using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// 
	/// </summary>
	internal class GossipChannelSelector : IChannelSelector {
		private static readonly ClusterMessages.VNodeState[] NotAllowedStates = {
			ClusterMessages.VNodeState.Manager,
			ClusterMessages.VNodeState.ShuttingDown,
			ClusterMessages.VNodeState.Shutdown,
			ClusterMessages.VNodeState.Unknown,
			ClusterMessages.VNodeState.Initializing,
			ClusterMessages.VNodeState.CatchingUp,
			ClusterMessages.VNodeState.ResigningLeader,
			ClusterMessages.VNodeState.PreLeader,
			ClusterMessages.VNodeState.PreReplica,
			ClusterMessages.VNodeState.Clone,
			ClusterMessages.VNodeState.DiscoverLeader
		};

		private readonly EventStoreClientSettings _settings;
		private readonly IGossipClient _gossipClient;
		private readonly CancellationToken _cancellationToken;
		private readonly Dictionary<DnsEndPoint, ChannelBase> _grpcChannelsCache;
		private readonly System.Threading.Channels.Channel<ChannelInfo> _channelInfoChannel;
		private readonly System.Threading.Channels.Channel<ControlMessage> _controlChannel;
		private readonly ILogger<GossipChannelSelector> _log;
		private readonly IComparer<ClusterMessages.VNodeState>? _comparer;

		private int _disposed;

		public GossipChannelSelector(EventStoreClientSettings settings, IGossipClient gossipClient,
			CancellationToken cancellationToken = default) {
			_settings = settings;
			_gossipClient = gossipClient;
			_cancellationToken = cancellationToken;
			_grpcChannelsCache = new Dictionary<DnsEndPoint, ChannelBase>();
			_channelInfoChannel = System.Threading.Channels.Channel.CreateUnbounded<ChannelInfo>();
			_controlChannel = System.Threading.Channels.Channel.CreateUnbounded<ControlMessage>();
			_log = settings.LoggerFactory?.CreateLogger<GossipChannelSelector>() ??
			       new NullLogger<GossipChannelSelector>();
			_comparer = settings.ConnectivitySettings.NodePreference switch {
				NodePreference.Leader => NodePreferenceComparers.Leader,
				NodePreference.Follower => NodePreferenceComparers.Follower,
				NodePreference.ReadOnlyReplica => NodePreferenceComparers.ReadOnlyReplica,
				_ => NodePreferenceComparers.Random
			};

			foreach (var endPoint in settings.ConnectivitySettings.GossipSeeds) {
				_grpcChannelsCache.Add(endPoint switch {
					DnsEndPoint dnsEndPoint => dnsEndPoint,
					_ => new DnsEndPoint(endPoint.GetHost(), endPoint.GetPort())
				}, ChannelFactory.CreateChannel(settings, endPoint));
			}

			_controlChannel.Writer.TryWrite(ControlMessage.Discover.Instance);

			_ = Task.Run(async () => {
				while (await _controlChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
					var message = await _controlChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

					if (message is ControlMessage.SetEndPoint sep) {
						await UseEndPoint(sep.EndPoint).ConfigureAwait(false);
					} else {
						var (success, endPoint) = await Discover(cancellationToken).ConfigureAwait(false);
						if (!success) {
							_log.LogError("Failed to discover candidate in {maxDiscoverAttempts} attempts.",
								settings.ConnectivitySettings.MaxDiscoverAttempts);

							_channelInfoChannel.Writer.TryComplete(
								new DiscoveryException(settings.ConnectivitySettings.MaxDiscoverAttempts));
							_controlChannel.Writer.TryComplete();
							return;
						}

						_log.LogInformation("Successfully discovered candidate at {endPoint}.", endPoint);

						await UseEndPoint(endPoint!).ConfigureAwait(false);
					}
				}
			}, cancellationToken);
		}

		private async ValueTask<(bool success, DnsEndPoint? endPoint)> Discover(CancellationToken cancellationToken) {
			for (var attempt = 1; attempt <= _settings.ConnectivitySettings.MaxDiscoverAttempts; attempt++) {
				var (success, endPoint, nodes) = await TryDiscover(attempt).ConfigureAwait(false);

				if (success) {
					await RemoveDeadNodes().ConfigureAwait(false);

					return (true, endPoint);
				}

				async ValueTask RemoveDeadNodes() {
					foreach (var dead in _grpcChannelsCache.Keys.Except(nodes!.Select(node => node.EndPoint),
						         DnsEndPointEqualityComparer.Instance).ToArray()) {
						if (!_grpcChannelsCache.TryGetValue(dead, out var channel)) {
							continue;
						}

						_grpcChannelsCache.Remove(dead);
						await channel.DisposeAsync().ConfigureAwait(false);
					}
				}
			}

			return (false, default);

			async ValueTask<(bool, DnsEndPoint?, ClusterMessages.MemberInfo[]?)> TryDiscover(int attempt) {
				foreach (var channel in _grpcChannelsCache.Values.ToArray().OrderBy(_ => Guid.NewGuid())) {
					try {
						var clusterInfo = await _gossipClient.GetAsync(channel, cancellationToken)
							.ConfigureAwait(false);

						if (clusterInfo.Members.Length == 0) {
							continue;
						}

						var nodes = clusterInfo.Members.Where(IsCandidate).ToArray();

						var node = nodes.OrderBy(node => node.State, _comparer)
							.ThenBy(_ => Guid.NewGuid())
							.FirstOrDefault();

						if (node is null) {
							continue;
						}

						return (true, node.EndPoint, nodes);
					} catch (Exception ex) {
						_log.Log(attempt switch {
								1 => LogLevel.Warning,
								_ when attempt == _settings.ConnectivitySettings.MaxDiscoverAttempts => LogLevel
									.Error,
								_ => LogLevel.Debug
							}, ex,
							"Could not discover candidate. Attempts remaining: {discoverAttempts}",
							_settings.ConnectivitySettings.MaxDiscoverAttempts - attempt);
					}
				}

				await Task.Delay(_settings.ConnectivitySettings.DiscoveryInterval, cancellationToken)
					.ConfigureAwait(false);

				return (false, default, default);
			}
		}

		private static bool IsCandidate(ClusterMessages.MemberInfo node) => node.IsAlive &&
		                                                                    !NotAllowedStates.Contains(node.State);

		public async ValueTask<ChannelInfo> SelectChannel(CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				return await _channelInfoChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
			} catch (Exception ex) when (ex.InnerException is DiscoveryException dex) {
				throw dex;
			}
		}

		public void Rediscover(DnsEndPoint? leader) {
			ThrowIfDisposed();

			var message = leader is null
				? (ControlMessage)ControlMessage.Discover.Instance
				: new ControlMessage.SetEndPoint(leader);

			_controlChannel.Writer.TryWrite(message);
		}

		private void ThrowIfDisposed() {
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0) {
				throw new ObjectDisposedException(GetType().ToString());
			}
		}

		public async ValueTask DisposeAsync() {
			if (Interlocked.Exchange(ref _disposed, 1) == 1) {
				return;
			}

			_channelInfoChannel.Writer.TryComplete();
			foreach (var channel in _grpcChannelsCache.Values) {
				await channel.DisposeAsync().ConfigureAwait(false);
			}
		}

		private async Task UseEndPoint(DnsEndPoint endPoint) {
			if (!_grpcChannelsCache.TryGetValue(endPoint, out var grpcChannel)) {
				grpcChannel = ChannelFactory.CreateChannel(_settings, endPoint);
				_grpcChannelsCache.Add(endPoint, grpcChannel);
			}

			await _channelInfoChannel.Writer.WriteAsync(new(grpcChannel, endPoint), _cancellationToken)
				.ConfigureAwait(false);
		}

		private record ControlMessage {
			public record SetEndPoint(DnsEndPoint EndPoint) : ControlMessage;

			public record Discover : ControlMessage {
				public static readonly Discover Instance = new();
			}
		}

		private class DnsEndPointEqualityComparer : IEqualityComparer<DnsEndPoint> {
			public static readonly DnsEndPointEqualityComparer Instance = new();

			public bool Equals(DnsEndPoint? x, DnsEndPoint? y) {
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				return string.Equals(x.Host, y.Host, StringComparison.OrdinalIgnoreCase) && x.Port == y.Port;
			}

			public int GetHashCode(DnsEndPoint obj) {
				unchecked {
					return (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Host) * 397) ^
					       obj.Port;
				}
			}
		}
	}
}
