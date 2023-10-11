using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Timeout_ = System.Threading.Timeout;

namespace EventStore.Client {
	public partial class EventStoreClientSettings {
		/// <summary>
		/// Creates client settings from a connection string
		/// </summary>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public static EventStoreClientSettings Create(string connectionString) =>
			ConnectionStringParser.Parse(connectionString);

		private static class ConnectionStringParser {
			private const string SchemeSeparator = "://";
			private const string UserInfoSeparator = "@";
			private const string Colon = ":";
			private const string Slash = "/";
			private const string Comma = ",";
			private const string Ampersand = "&";
			private const string Equal = "=";
			private const string QuestionMark = "?";

			private const string Tls = nameof(Tls);
			private const string ConnectionName = nameof(ConnectionName);
			private const string MaxDiscoverAttempts = nameof(MaxDiscoverAttempts);
			private const string DiscoveryInterval = nameof(DiscoveryInterval);
			private const string GossipTimeout = nameof(GossipTimeout);
			private const string NodePreference = nameof(NodePreference);
			private const string TlsVerifyCert = nameof(TlsVerifyCert);
			private const string DefaultDeadline = nameof(DefaultDeadline);
			private const string ThrowOnAppendFailure = nameof(ThrowOnAppendFailure);
			private const string KeepAliveInterval = nameof(KeepAliveInterval);
			private const string KeepAliveTimeout = nameof(KeepAliveTimeout);

			private const string UriSchemeDiscover = "esdb+discover";

			private static readonly string[] Schemes = {"esdb", UriSchemeDiscover};
			private static readonly int DefaultPort = EventStoreClientConnectivitySettings.Default.Address.Port;
			private static readonly bool DefaultUseTls = true;

			private static readonly Dictionary<string, Type> SettingsType =
				new(StringComparer.InvariantCultureIgnoreCase) {
					{ConnectionName, typeof(string)},
					{MaxDiscoverAttempts, typeof(int)},
					{DiscoveryInterval, typeof(int)},
					{GossipTimeout, typeof(int)},
					{NodePreference, typeof(string)},
					{Tls, typeof(bool)},
					{TlsVerifyCert, typeof(bool)},
					{DefaultDeadline, typeof(int)},
					{ThrowOnAppendFailure, typeof(bool)},
					{KeepAliveInterval, typeof(int)},
					{KeepAliveTimeout, typeof(int)},
				};

			public static EventStoreClientSettings Parse(string connectionString) {
				var currentIndex = 0;
				var schemeIndex = connectionString.IndexOf(SchemeSeparator, currentIndex, StringComparison.Ordinal);
				if (schemeIndex == -1)
					throw new NoSchemeException();
				var scheme = ParseScheme(connectionString.Substring(0, schemeIndex));

				currentIndex = schemeIndex + SchemeSeparator.Length;
				var userInfoIndex = connectionString.IndexOf(UserInfoSeparator, currentIndex, StringComparison.Ordinal);
				(string user, string pass)? userInfo = null;
				if (userInfoIndex != -1) {
					userInfo = ParseUserInfo(connectionString.Substring(currentIndex, userInfoIndex - currentIndex));
					currentIndex = userInfoIndex + UserInfoSeparator.Length;
				}


				var slashIndex = connectionString.IndexOf(Slash, currentIndex, StringComparison.Ordinal);
				var questionMarkIndex = connectionString.IndexOf(QuestionMark, Math.Max(currentIndex, slashIndex),
					StringComparison.Ordinal);
				var endIndex = connectionString.Length;

				//for simpler substring operations:
				if (slashIndex == -1) slashIndex = int.MaxValue;
				if (questionMarkIndex == -1) questionMarkIndex = int.MaxValue;

				var hostSeparatorIndex = Math.Min(Math.Min(slashIndex, questionMarkIndex), endIndex);
				var hosts = ParseHosts(connectionString.Substring(currentIndex, hostSeparatorIndex - currentIndex));
				currentIndex = hostSeparatorIndex;

				string path = "";
				if (slashIndex != int.MaxValue)
					path = connectionString.Substring(currentIndex,
						Math.Min(questionMarkIndex, endIndex) - currentIndex);

				if (path != "" && path != "/")
					throw new ConnectionStringParseException(
						$"The specified path must be either an empty string or a forward slash (/) but the following path was found instead: '{path}'");

				var options = new Dictionary<string, string>();
				if (questionMarkIndex != int.MaxValue) {
					currentIndex = questionMarkIndex + QuestionMark.Length;
					options = ParseKeyValuePairs(connectionString.Substring(currentIndex));
				}

				return CreateSettings(scheme, userInfo, hosts, options);
			}

			private static EventStoreClientSettings CreateSettings(string scheme, (string user, string pass)? userInfo,
				EndPoint[] hosts, Dictionary<string, string> options) {
				var settings = new EventStoreClientSettings {
					ConnectivitySettings = EventStoreClientConnectivitySettings.Default,
					OperationOptions = EventStoreClientOperationOptions.Default
				};

				if (userInfo.HasValue)
					settings.DefaultCredentials = new UserCredentials(userInfo.Value.user, userInfo.Value.pass);

				var typedOptions = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
				foreach (var kv in options) {
					if (!SettingsType.TryGetValue(kv.Key, out var type))
						throw new InvalidSettingException($"Unknown option: {kv.Key}");
					if (type == typeof(int)) {
						if (!int.TryParse(kv.Value, out var intValue))
							throw new InvalidSettingException($"{kv.Key} must be an integer value");
						typedOptions.Add(kv.Key, intValue);
					} else if (type == typeof(bool)) {
						if (!bool.TryParse(kv.Value, out var boolValue))
							throw new InvalidSettingException($"{kv.Key} must be either true or false");
						typedOptions.Add(kv.Key, boolValue);
					} else if (type == typeof(string)) {
						typedOptions.Add(kv.Key, kv.Value);
					}
				}

				if (typedOptions.TryGetValue(ConnectionName, out object? connectionName))
					settings.ConnectionName = (string)connectionName;

				var connSettings = settings.ConnectivitySettings;

				if (typedOptions.TryGetValue(MaxDiscoverAttempts, out object? maxDiscoverAttempts))
					connSettings.MaxDiscoverAttempts = (int)maxDiscoverAttempts;

				if (typedOptions.TryGetValue(DiscoveryInterval, out object? discoveryInterval))
					connSettings.DiscoveryInterval = TimeSpan.FromMilliseconds((int)discoveryInterval);

				if (typedOptions.TryGetValue(GossipTimeout, out object? gossipTimeout))
					connSettings.GossipTimeout = TimeSpan.FromMilliseconds((int)gossipTimeout);

				if (typedOptions.TryGetValue(NodePreference, out object? nodePreference)) {
					connSettings.NodePreference = ((string)nodePreference).ToLowerInvariant() switch {
						"leader" => EventStore.Client.NodePreference.Leader,
						"follower" => EventStore.Client.NodePreference.Follower,
						"random" => EventStore.Client.NodePreference.Random,
						"readonlyreplica" => EventStore.Client.NodePreference.ReadOnlyReplica,
						_ => throw new InvalidSettingException($"Invalid NodePreference: {nodePreference}")
					};
				}

				var useTls = DefaultUseTls;
				if (typedOptions.TryGetValue(Tls, out object? tls)) {
					useTls = (bool)tls;
				}

				if (typedOptions.TryGetValue(DefaultDeadline, out object? operationTimeout))
					settings.DefaultDeadline = TimeSpan.FromMilliseconds((int)operationTimeout);

				if (typedOptions.TryGetValue(ThrowOnAppendFailure, out object? throwOnAppendFailure))
					settings.OperationOptions.ThrowOnAppendFailure = (bool)throwOnAppendFailure;

				if (typedOptions.TryGetValue(KeepAliveInterval, out var keepAliveIntervalMs)) {
					settings.ConnectivitySettings.KeepAliveInterval = keepAliveIntervalMs switch {
						-1 => Timeout_.InfiniteTimeSpan,
						int value and >= 0 => TimeSpan.FromMilliseconds(value),
						_ => throw new InvalidSettingException($"Invalid KeepAliveInterval: {keepAliveIntervalMs}")
					};
				}

				if (typedOptions.TryGetValue(KeepAliveTimeout, out var keepAliveTimeoutMs)) {
					settings.ConnectivitySettings.KeepAliveTimeout = keepAliveTimeoutMs switch {
						-1 => Timeout_.InfiniteTimeSpan,
						int value and >= 0 => TimeSpan.FromMilliseconds(value),
						_ => throw new InvalidSettingException($"Invalid KeepAliveTimeout: {keepAliveTimeoutMs}")
					};
				}

				connSettings.Insecure = !useTls;

				if (hosts.Length == 1 && scheme != UriSchemeDiscover) {
					connSettings.Address = hosts[0].ToUri(useTls);
				} else {
					if (hosts.Any(x => x is DnsEndPoint))
						connSettings.DnsGossipSeeds =
							Array.ConvertAll(hosts, x => new DnsEndPoint(x.GetHost(), x.GetPort()));
					else
						connSettings.IpGossipSeeds = Array.ConvertAll(hosts, x => (IPEndPoint)x);
				}

				if (typedOptions.TryGetValue(TlsVerifyCert, out var tlsVerifyCert)) {
					settings.ConnectivitySettings.TlsVerifyCert = (bool)tlsVerifyCert;
				}

                settings.CreateHttpMessageHandler = CreateDefaultHandler;

				return settings;

                HttpMessageHandler CreateDefaultHandler() {
#if NET
					var handler = new SocketsHttpHandler {
						KeepAlivePingDelay = settings.ConnectivitySettings.KeepAliveInterval,
						KeepAlivePingTimeout = settings.ConnectivitySettings.KeepAliveTimeout,
						EnableMultipleHttp2Connections = true,
					};

					if (!settings.ConnectivitySettings.TlsVerifyCert) {
						handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
					}
#else
                    var handler = new WinHttpHandler {
                        TcpKeepAliveEnabled = true,
                        TcpKeepAliveTime = settings.ConnectivitySettings.KeepAliveTimeout,
                        TcpKeepAliveInterval = settings.ConnectivitySettings.KeepAliveInterval,
                        EnableMultipleHttp2Connections = true
                    };
                    
					if (!settings.ConnectivitySettings.TlsVerifyCert) {
						handler.ServerCertificateValidationCallback = delegate { return true; };
					}
#endif
					return handler;
                }
			}

			private static string ParseScheme(string s) =>
				!Schemes.Contains(s) ? throw new InvalidSchemeException(s, Schemes) : s;

			private static (string, string) ParseUserInfo(string s) {
				var tokens = s.Split(Colon[0]);
				if (tokens.Length != 2) throw new InvalidUserCredentialsException(s);
				return (tokens[0], tokens[1]);
			}

			private static EndPoint[] ParseHosts(string s) {
				var hostsTokens = s.Split(Comma[0]);
				var hosts = new List<EndPoint>();
				foreach (var hostToken in hostsTokens) {
					var hostPortToken = hostToken.Split(Colon[0]);
					string host;
					int port;
					switch (hostPortToken.Length) {
						case 1:
							host = hostPortToken[0];
							port = DefaultPort;
							break;
						case 2: {
							host = hostPortToken[0];
							if (!int.TryParse(hostPortToken[1], out port))
								throw new InvalidHostException(hostToken);
							break;
						}
						default:
							throw new InvalidHostException(hostToken);
					}

					if (host.Length == 0) {
						throw new InvalidHostException(hostToken);
					}

					if (IPAddress.TryParse(host, out IPAddress? ip)) {
						hosts.Add(new IPEndPoint(ip, port));
					} else {
						hosts.Add(new DnsEndPoint(host, port));
					}
				}

				return hosts.ToArray();
			}

			private static Dictionary<string, string> ParseKeyValuePairs(string s) {
				var options = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
				var optionsTokens = s.Split(Ampersand[0]);
				foreach (var optionToken in optionsTokens) {
					var (key, val) = ParseKeyValuePair(optionToken);
					try {
						options.Add(key, val);
					} catch (ArgumentException) {
						throw new DuplicateKeyException(key);
					}
				}

				return options;
			}

			private static (string, string) ParseKeyValuePair(string s) {
				var keyValueToken = s.Split(Equal[0]);
				if (keyValueToken.Length != 2) {
					throw new InvalidKeyValuePairException(s);
				}

				return (keyValueToken[0], keyValueToken[1]);
			}
		}
	}
}
