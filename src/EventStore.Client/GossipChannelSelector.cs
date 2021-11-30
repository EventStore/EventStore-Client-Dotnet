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
	internal class GossipChannelSelector : IChannelSelector {
		private readonly EventStoreClientSettings _settings;
		private readonly IServerCapabilitiesClient _serverCapabilities;
		private readonly CancellationToken _cancellationToken;

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

		private readonly Dictionary<EndPoint, ChannelBase> _grpcChannelsCache;
		private readonly Dictionary<EndPoint, ServerCapabilities> _serverCapabilitiesCache;
		private readonly System.Threading.Channels.Channel<ChannelInfo> _channelInfoChannel;
		private readonly System.Threading.Channels.Channel<ControlMessage> _controlChannel;
		private readonly ILogger<GossipChannelSelector> _log;
		private int _disposed;

		public GossipChannelSelector(EventStoreClientSettings settings, IGossipClient gossipClient,
			IServerCapabilitiesClient serverCapabilities, CancellationToken cancellationToken = default) {
			_settings = settings;
			_serverCapabilities = serverCapabilities;
			_cancellationToken = cancellationToken;
			_grpcChannelsCache = new Dictionary<EndPoint, ChannelBase>();
			_serverCapabilitiesCache = new Dictionary<EndPoint, ServerCapabilities>();
			_channelInfoChannel = System.Threading.Channels.Channel.CreateUnbounded<ChannelInfo>();
			_controlChannel = System.Threading.Channels.Channel.CreateUnbounded<ControlMessage>();
			_log = settings.LoggerFactory?.CreateLogger<GossipChannelSelector>() ??
			       new NullLogger<GossipChannelSelector>();

			foreach (var endPoint in settings.ConnectivitySettings.GossipSeeds) {
				_grpcChannelsCache.Add(endPoint, ChannelFactory.CreateChannel(settings, endPoint));
			}

			_controlChannel.Writer.TryWrite(ControlMessage.Discover.Instance);

			_ = Task.Run(async () => {
				var comparer = settings.ConnectivitySettings.NodePreference switch {
					NodePreference.Leader => NodePreferenceComparers.Leader,
					NodePreference.Follower => NodePreferenceComparers.Follower,
					NodePreference.ReadOnlyReplica => NodePreferenceComparers.ReadOnlyReplica,
					_ => NodePreferenceComparers.Random
				};

				Loop:
				while (await _controlChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
					var message = await _controlChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

					if (message is ControlMessage.SetEndPoint setEndPoint) {
						await UseEndPoint(setEndPoint.EndPoint).ConfigureAwait(false);
					} else {
						for (var attempt = 1; attempt <= settings.ConnectivitySettings.MaxDiscoverAttempts; attempt++) {
							var (success, endPoint, nodes) = await TryDiscover(attempt).ConfigureAwait(false);

							if (!success) {
								continue;
							}
							
							await UseEndPoint(endPoint!).ConfigureAwait(false);

							foreach (var endpoint in _grpcChannelsCache.Keys.ToArray()) {
								if (nodes!.Any(node =>
									node.EndPoint.Host == endpoint.GetHost() &&
									node.EndPoint.Port == endpoint.GetPort())) {
									continue;
								}

								if (!_grpcChannelsCache.TryGetValue(endpoint, out var channel)) {
									continue;
								}

								_grpcChannelsCache.Remove(endpoint);
								await channel.DisposeAsync().ConfigureAwait(false);
							}

							goto Loop;
						}

						_log.LogError("Failed to discover candidate in {maxDiscoverAttempts} attempts.",
							settings.ConnectivitySettings.MaxDiscoverAttempts);

						_channelInfoChannel.Writer.TryComplete(
							new DiscoveryException(settings.ConnectivitySettings.MaxDiscoverAttempts));
						_controlChannel.Writer.TryComplete();
					}
				}

				async ValueTask<(bool, EndPoint?, ClusterMessages.MemberInfo[]?)> TryDiscover(int attempt) {
					foreach (var channel in _grpcChannelsCache.Values.ToArray().OrderBy(_ => Guid.NewGuid())) {
						try {
							var clusterInfo = await gossipClient.GetAsync(channel, cancellationToken)
								.ConfigureAwait(false);

							if (clusterInfo.Members.Length == 0) {
								continue;
							}

							var nodes = clusterInfo.Members.Where(IsCandidate).ToArray();
							
							var node = nodes.OrderBy(node => node.State, comparer)
								.ThenBy(_ => Guid.NewGuid())
								.FirstOrDefault();

							if (node is null) {
								continue;
							}
							return (true, node.EndPoint, nodes);
						} catch (Exception ex) {
							_log.Log(attempt switch {
									1 => LogLevel.Warning,
									_ when attempt == _settings.ConnectivitySettings.MaxDiscoverAttempts => LogLevel.Error,
									_ => LogLevel.Debug
								},  ex,
								"Could not discover candidate. Attempts remaining: {discoverAttempts}",
								_settings.ConnectivitySettings.MaxDiscoverAttempts - 1);
						}
					}

					await Task.Delay(settings.ConnectivitySettings.DiscoveryInterval, cancellationToken)
						.ConfigureAwait(false);

					return (false, default, default);
				}
			}, cancellationToken);
		}

		private static bool IsCandidate(ClusterMessages.MemberInfo node) => node.IsAlive &&
		                                                                    !NotAllowedStates.Contains(node.State);

		public async Task<ChannelInfo> SelectChannel(CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				return await _channelInfoChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
			} catch (Exception ex) when (ex.InnerException is DiscoveryException dex) {
				throw dex;
			}
		}

		public void SetEndPoint(EndPoint leader) {
			ThrowIfDisposed();

			_controlChannel.Writer.TryWrite(new ControlMessage.SetEndPoint(leader));
		}

		public void Rediscover() {
			ThrowIfDisposed();

			_controlChannel.Writer.TryWrite(ControlMessage.Discover.Instance);
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

		private async Task UseEndPoint(EndPoint endPoint) {
			if (!_grpcChannelsCache.TryGetValue(endPoint, out var grpcChannel)) {
				grpcChannel = ChannelFactory.CreateChannel(_settings, endPoint);
				_grpcChannelsCache.Add(endPoint, grpcChannel);
			}

			if (!_serverCapabilitiesCache.TryGetValue(endPoint, out var serverCapabilities)) {
				serverCapabilities = await _serverCapabilities.GetAsync(grpcChannel, _cancellationToken)
					.ConfigureAwait(false);
				_serverCapabilitiesCache.Add(endPoint, serverCapabilities);
			}

			await _channelInfoChannel.Writer.WriteAsync(new(grpcChannel, serverCapabilities), _cancellationToken)
				.ConfigureAwait(false);
		}

		private record ControlMessage {
			public record SetEndPoint(EndPoint EndPoint) : ControlMessage;

			public record Discover : ControlMessage {
				public static readonly Discover Instance = new();
			}
		}
	}
}
