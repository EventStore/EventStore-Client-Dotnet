using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace EventStore.Client {
	public partial class EventStoreClientSettings {
		/// <summary>
		/// Creates client settings from a connection string
		/// </summary>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public static EventStoreClientSettings Create(string connectionString) {
			return ConnectionStringParser.Parse(connectionString);
		}

		private static class ConnectionStringParser {
			private const string SchemeSeparator = "://";
			private const string UserInfoSeparator = "@";
			private const string Colon = ":";
			private const string Slash = "/";
			private const string Comma = ",";
			private const string Ampersand = "&";
			private const string Equal = "=";
			private const string QuestionMark = "?";
			private static readonly string[] Schemes = { "esdb" };
			private static readonly int DefaultPort = EventStoreClientConnectivitySettings.Default.Address.Port;
			private static readonly bool DefaultUseTls = true;

			private static readonly Dictionary<string, Type> SettingsType = new Dictionary<string, Type> (StringComparer.InvariantCultureIgnoreCase) {
				{"ConnectionName", typeof(string)},
				{"MaxDiscoverAttempts", typeof(int)},
				{"DiscoveryInterval", typeof(int)},
				{"GossipTimeout", typeof(int)},
				{"NodePreference", typeof(string)},
				{"Tls", typeof(bool)},
				{"TlsVerifyCert", typeof(bool)},
				{"OperationTimeout", typeof(int)},
				{"ThrowOnAppendFailure", typeof(bool)}
			};

			public static EventStoreClientSettings Parse(string connectionString) {
				var currentIndex = 0;
				var schemeIndex = connectionString.IndexOf(SchemeSeparator, currentIndex, StringComparison.Ordinal);
				if (schemeIndex == -1)
					throw new NoSchemeException();
				var scheme = ParseScheme(connectionString.Substring(0, schemeIndex));

				currentIndex = schemeIndex + SchemeSeparator.Length;
				var userInfoIndex = connectionString.IndexOf(UserInfoSeparator, currentIndex, StringComparison.Ordinal);
				(string user, string pass) userInfo = (null, null);
				if (userInfoIndex != -1) {
					userInfo = ParseUserInfo(connectionString.Substring(currentIndex, userInfoIndex - currentIndex));
					currentIndex = userInfoIndex + UserInfoSeparator.Length;
				}


				var slashIndex = connectionString.IndexOf(Slash, currentIndex, StringComparison.Ordinal);
				var questionMarkIndex = connectionString.IndexOf(QuestionMark, Math.Max(currentIndex, slashIndex), StringComparison.Ordinal);
				var endIndex = connectionString.Length;

				//for simpler substring operations:
				if (slashIndex == -1) slashIndex = int.MaxValue;
				if (questionMarkIndex == -1) questionMarkIndex = int.MaxValue;

				var hostSeparatorIndex = Math.Min(Math.Min(slashIndex, questionMarkIndex), endIndex);
				var hosts = ParseHosts(connectionString.Substring(currentIndex,hostSeparatorIndex - currentIndex));
				currentIndex = hostSeparatorIndex;

				string path = "";
				if (slashIndex != int.MaxValue)
					path = connectionString.Substring(currentIndex,Math.Min(questionMarkIndex, endIndex) - currentIndex);

				if (path != "" && path != "/")
					throw new ConnectionStringParseException($"The specified path must be either an empty string or a forward slash (/) but the following path was found instead: '{path}'");

				var options = new Dictionary<string, string>();
				if (questionMarkIndex != int.MaxValue) {
					currentIndex = questionMarkIndex + QuestionMark.Length;
					options = ParseKeyValuePairs(connectionString.Substring(currentIndex));
				}

				return CreateSettings(scheme, userInfo, hosts, options);
			}

			private static EventStoreClientSettings CreateSettings(string scheme, (string user, string pass) userInfo, EndPoint[] hosts, Dictionary<string, string> options) {
				var settings = new EventStoreClientSettings {
					ConnectivitySettings = EventStoreClientConnectivitySettings.Default,
					OperationOptions = EventStoreClientOperationOptions.Default
				};

				if (userInfo != (null, null))
					settings.DefaultCredentials = new UserCredentials(userInfo.user, userInfo.pass);

				var typedOptions = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
				foreach (var option in options) {
					if (!SettingsType.TryGetValue(option.Key, out var type)) throw new InvalidSettingException($"Unknown option: {option.Key}");
					if(type == typeof(int)){
						if (!int.TryParse(option.Value, out var intValue))
							throw new InvalidSettingException($"{option.Key} must be an integer value");
						typedOptions.Add(option.Key, intValue);
					} else if (type == typeof(bool)) {
						if (!bool.TryParse(option.Value, out var boolValue))
							throw new InvalidSettingException($"{option.Key} must be either true or false");
						typedOptions.Add(option.Key, boolValue);
					} else if (type == typeof(string)) {
						typedOptions.Add(option.Key, option.Value);
					}
				}

				if (typedOptions.TryGetValue("ConnectionName", out object connectionName))
					settings.ConnectionName = (string) connectionName;

				var connSettings = settings.ConnectivitySettings;

				if (typedOptions.TryGetValue("MaxDiscoverAttempts", out object maxDiscoverAttempts))
					connSettings.MaxDiscoverAttempts = (int)maxDiscoverAttempts;

				if (typedOptions.TryGetValue("DiscoveryInterval", out object discoveryInterval))
					connSettings.DiscoveryInterval = TimeSpan.FromMilliseconds((int)discoveryInterval);

				if (typedOptions.TryGetValue("GossipTimeout", out object gossipTimeout))
					connSettings.GossipTimeout = TimeSpan.FromMilliseconds((int)gossipTimeout);

				if (typedOptions.TryGetValue("NodePreference", out object nodePreference)) {
					var nodePreferenceLowerCase = ((string)nodePreference).ToLowerInvariant();
					switch (nodePreferenceLowerCase) {
						case "leader":
							connSettings.NodePreference = NodePreference.Leader;
							break;
						case "follower":
							connSettings.NodePreference = NodePreference.Follower;
							break;
						case "random":
							connSettings.NodePreference = NodePreference.Random;
							break;
						case "readonlyreplica":
							connSettings.NodePreference = NodePreference.ReadOnlyReplica;
							break;
						default:
							throw new InvalidSettingException($"Invalid NodePreference: {nodePreference}");
					}
				}

				var useTls = DefaultUseTls;
				if(typedOptions.TryGetValue("Tls", out object tls)) {
					useTls = (bool)tls;
				}

				if (typedOptions.TryGetValue("TlsVerifyCert", out object tlsVerifyCert)) {
					if (!(bool)tlsVerifyCert) {
						settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
							SslOptions = {
								RemoteCertificateValidationCallback = delegate { return true; }
							}
						};
					}
				}

				if(typedOptions.TryGetValue("OperationTimeout", out object operationTimeout))
					settings.OperationOptions.TimeoutAfter = TimeSpan.FromMilliseconds((int) operationTimeout);

				if(typedOptions.TryGetValue("ThrowOnAppendFailure", out object throwOnAppendFailure))
					settings.OperationOptions.ThrowOnAppendFailure = (bool) throwOnAppendFailure;

				if (hosts.Length == 1) {
					connSettings.Address = new Uri(hosts[0].ToHttpUrl(useTls?Uri.UriSchemeHttps:Uri.UriSchemeHttp));
				} else {
					if (hosts.Any(x => x is DnsEndPoint))
						connSettings.DnsGossipSeeds = hosts.Select(x => new DnsEndPoint(x.GetHost(), x.GetPort())).ToArray();
					else
						connSettings.IpGossipSeeds = hosts.Select(x => x as IPEndPoint).ToArray();

					connSettings.GossipOverHttps = useTls;
				}

				return settings;
			}

			private static string ParseScheme(string s) {
				if (!Schemes.Contains(s)) throw new InvalidSchemeException(s, Schemes);
				return s;
			}

			private static (string,string) ParseUserInfo(string s) {
				var tokens = s.Split(Colon);
				if (tokens.Length != 2) throw new InvalidUserCredentialsException(s);
				return (tokens[0], tokens[1]);
			}

			private static EndPoint[] ParseHosts(string s) {
				var hostsTokens = s.Split(Comma);
				var hosts = new List<EndPoint>();
				foreach (var hostToken in hostsTokens) {
					var hostPortToken = hostToken.Split(Colon);
					string host;
					int port;
					switch (hostPortToken.Length)
					{
						case 1:
							host = hostPortToken[0];
							port = DefaultPort;
							break;
						case 2:
						{
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

					if (IPAddress.TryParse(host, out IPAddress ip)) {
						hosts.Add(new IPEndPoint(ip, port));
					} else {
						hosts.Add(new DnsEndPoint(host, port));
					}
				}

				return hosts.ToArray();
			}

			private static Dictionary<string, string> ParseKeyValuePairs(string s) {
				var options = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
				var optionsTokens = s.Split(Ampersand);
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

			private static (string,string) ParseKeyValuePair(string s) {
				var keyValueToken = s.Split(Equal);
				if (keyValueToken.Length != 2) {
					throw new InvalidKeyValuePairException(s);
				}

				return (keyValueToken[0], keyValueToken[1]);
			}
		}
	}
}
